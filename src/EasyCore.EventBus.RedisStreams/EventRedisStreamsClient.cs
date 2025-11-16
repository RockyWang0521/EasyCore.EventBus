using EasyCore.EventBus.Event;
using EasyCore.RedisStreams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EasyCore.EventBus.RedisStreams
{
    /// <summary>
    /// EventBus adapter over <see cref="IRedisStreamsClient"/>.
    /// </summary>
    public sealed class EventRedisStreamsClient : IEventMessageQueueClient
    {
        private readonly IRedisStreamsClient _client;
        private readonly DistributedEventDispatcher _dispatcher;
        private readonly EventBusOptions _eventBusOptions;
        private readonly ILogger<EventRedisStreamsClient> _logger;
        private IReadOnlyDictionary<string, Type> _eventTypes = new Dictionary<string, Type>();

        public EventRedisStreamsClient(
            IRedisStreamsClient client,
            DistributedEventDispatcher dispatcher,
            IOptions<EventBusOptions> eventBusOptions,
            ILogger<EventRedisStreamsClient>? logger = null)
        {
            _client = client;
            _dispatcher = dispatcher;
            _eventBusOptions = eventBusOptions.Value;
            _logger = logger ?? NullLogger<EventRedisStreamsClient>.Instance;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default) =>
            _client.ConnectAsync(cancellationToken);

        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            _eventTypes = EventTypeScanner.GetDistributedEventTypes().ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
            await _client.SubscribeAsync(_eventTypes.Keys, OnMessageAsync, cancellationToken).ConfigureAwait(false);
        }

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent =>
            PublishAsync(eventMessage).GetAwaiter().GetResult();

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken = default)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(eventMessage);

            var typeName = eventMessage.GetType().Name;
            var payload = JsonSerializer.Serialize(eventMessage, eventMessage.GetType());
            var header = new RedisStreamHeader
            {
                RetryCount = _eventBusOptions.RetryCount,
                RetryInterval = _eventBusOptions.RetryInterval
            };

            await _client.PublishAsync(typeName, typeName, payload, header, cancellationToken).ConfigureAwait(false);
            return true;
        }

        private async Task OnMessageAsync(RedisStreamsDeliveredMessage message, CancellationToken cancellationToken)
        {
            if (!_eventTypes.TryGetValue(message.TypeName, out var eventType))
            {
                _logger.LogWarning("Unknown Redis Streams type {TypeName}, acknowledging to skip.", message.TypeName);
                await _client.AcknowledgeAsync(message.StreamKey, message.MessageId, cancellationToken).ConfigureAwait(false);
                return;
            }

            var maxRetry = message.Header.RetryCount;
            var interval = message.Header.RetryInterval;

            var result = await _dispatcher.DispatchAsync(eventType, message.PayloadJson, maxRetry, interval, cancellationToken)
                .ConfigureAwait(false);

            if (result is EventDispatchResult.Handled or EventDispatchResult.RetryExhausted or EventDispatchResult.NoHandler
                or EventDispatchResult.UnknownType)
            {
                await _client.AcknowledgeAsync(message.StreamKey, message.MessageId, cancellationToken).ConfigureAwait(false);
            }
        }

        public ValueTask DisposeAsync() => _client.DisposeAsync();
    }
}
