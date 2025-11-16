using EasyCore.EventBus.Event;
using EasyCore.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EasyCore.EventBus.Kafka
{
    /// <summary>
    /// EventBus adapter over <see cref="IKafkaClient"/>.
    /// </summary>
    public sealed class EventKafkaClient : IEventMessageQueueClient
    {
        private readonly IKafkaClient _client;
        private readonly KafkaOptions _kafkaOptions;
        private readonly DistributedEventDispatcher _dispatcher;
        private readonly EventBusOptions _eventBusOptions;
        private readonly ILogger<EventKafkaClient> _logger;
        private IReadOnlyDictionary<string, Type> _eventTypes = new Dictionary<string, Type>();

        public EventKafkaClient(
            IKafkaClient client,
            IOptions<KafkaOptions> kafkaOptions,
            DistributedEventDispatcher dispatcher,
            IOptions<EventBusOptions> eventBusOptions,
            ILogger<EventKafkaClient>? logger = null)
        {
            _client = client;
            _kafkaOptions = kafkaOptions.Value;
            _dispatcher = dispatcher;
            _eventBusOptions = eventBusOptions.Value;
            _logger = logger ?? NullLogger<EventKafkaClient>.Instance;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default) =>
            _client.ConnectAsync(cancellationToken);

        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            _eventTypes = EventTypeScanner.GetDistributedEventTypes().ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
            var topics = _eventTypes.Keys.Select(name => $"{_kafkaOptions.TopicName}.{name}").ToList();

            await _client.SubscribeAsync(topics, OnMessageAsync, cancellationToken).ConfigureAwait(false);
        }

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent =>
            PublishAsync(eventMessage).GetAwaiter().GetResult();

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken = default)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(eventMessage);

            var typeName = eventMessage.GetType().Name;
            var topic = $"{_kafkaOptions.TopicName}.{typeName}";
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventMessage, eventMessage.GetType()));
            var headers = new Dictionary<string, byte[]>
            {
                [EventMessageHeaders.EventType] = Encoding.UTF8.GetBytes(typeName),
                [EventMessageHeaders.Retry] = Encoding.UTF8.GetBytes(_eventBusOptions.RetryCount.ToString()),
                [EventMessageHeaders.RetryInterval] = Encoding.UTF8.GetBytes(_eventBusOptions.RetryInterval.ToString())
            };

            await _client.PublishAsync(topic, body, typeName, headers, cancellationToken).ConfigureAwait(false);
            return true;
        }

        private async Task OnMessageAsync(KafkaDeliveredMessage message, CancellationToken cancellationToken)
        {
            var stringHeaders = message.Headers.ToDictionary(
                kv => kv.Key,
                kv => Encoding.UTF8.GetString(kv.Value),
                StringComparer.Ordinal);

            if (!stringHeaders.TryGetValue(EventMessageHeaders.EventType, out var typeName)
                || !_eventTypes.TryGetValue(typeName, out var eventType))
            {
                _logger.LogWarning("Unknown Kafka event type on topic {Topic}, committing to skip.", message.Topic);
                if (message.NativeResult != null)
                    _client.Commit(message.NativeResult);
                return;
            }

            var payload = Encoding.UTF8.GetString(message.Body.Span);
            var (maxRetry, interval) = DistributedEventDispatcher.ParseRetryHeaders(stringHeaders, _eventBusOptions);

            var result = await _dispatcher.DispatchAsync(eventType, payload, maxRetry, interval, cancellationToken)
                .ConfigureAwait(false);

            if (result is EventDispatchResult.Handled or EventDispatchResult.RetryExhausted or EventDispatchResult.NoHandler
                or EventDispatchResult.UnknownType)
            {
                if (message.NativeResult != null)
                    _client.Commit(message.NativeResult);
            }
        }

        public ValueTask DisposeAsync() => _client.DisposeAsync();
    }
}
