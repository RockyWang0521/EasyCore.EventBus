using EasyCore.EventBus.Event;
using EasyCore.Pulsar;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EasyCore.EventBus.Pulsar
{
    /// <summary>
    /// EventBus adapter over <see cref="IPulsarClient"/>.
    /// </summary>
    public sealed class EventPulsarClient : IEventMessageQueueClient
    {
        private readonly IPulsarClient _client;
        private readonly DistributedEventDispatcher _dispatcher;
        private readonly EventBusOptions _eventBusOptions;
        private readonly ILogger<EventPulsarClient> _logger;
        private IReadOnlyDictionary<string, Type> _eventTypes = new Dictionary<string, Type>();

        public EventPulsarClient(
            IPulsarClient client,
            DistributedEventDispatcher dispatcher,
            IOptions<EventBusOptions> eventBusOptions,
            ILogger<EventPulsarClient>? logger = null)
        {
            _client = client;
            _dispatcher = dispatcher;
            _eventBusOptions = eventBusOptions.Value;
            _logger = logger ?? NullLogger<EventPulsarClient>.Instance;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default) =>
            _client.ConnectAsync(cancellationToken);

        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            _eventTypes = EventTypeScanner.GetDistributedEventTypes().ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
            var topics = _eventTypes.Keys.ToList();

            await _client.SubscribeAsync(topics, OnMessageAsync, cancellationToken).ConfigureAwait(false);
        }

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent =>
            PublishAsync(eventMessage).GetAwaiter().GetResult();

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken = default)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(eventMessage);

            var typeName = eventMessage.GetType().Name;
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventMessage, eventMessage.GetType()));
            var properties = new Dictionary<string, string>
            {
                [EventMessageHeaders.EventType] = typeName,
                [EventMessageHeaders.Retry] = _eventBusOptions.RetryCount.ToString(),
                [EventMessageHeaders.RetryInterval] = _eventBusOptions.RetryInterval.ToString()
            };

            await _client.PublishAsync(typeName, body, properties, cancellationToken).ConfigureAwait(false);
            return true;
        }

        private async Task OnMessageAsync(PulsarDeliveredMessage message, CancellationToken cancellationToken)
        {
            if (!message.Properties.TryGetValue(EventMessageHeaders.EventType, out var typeName)
                || !_eventTypes.TryGetValue(typeName, out var eventType))
            {
                _logger.LogWarning("Unknown Pulsar event type, acknowledging to skip.");
                await _client.AcknowledgeAsync(message.MessageId, cancellationToken).ConfigureAwait(false);
                return;
            }

            var payload = Encoding.UTF8.GetString(message.Body.Span);
            var (maxRetry, interval) = DistributedEventDispatcher.ParseRetryHeaders(message.Properties, _eventBusOptions);

            var result = await _dispatcher.DispatchAsync(eventType, payload, maxRetry, interval, cancellationToken)
                .ConfigureAwait(false);

            if (result is EventDispatchResult.Handled or EventDispatchResult.RetryExhausted or EventDispatchResult.NoHandler
                or EventDispatchResult.UnknownType)
            {
                await _client.AcknowledgeAsync(message.MessageId, cancellationToken).ConfigureAwait(false);
            }
        }

        public ValueTask DisposeAsync() => _client.DisposeAsync();
    }
}
