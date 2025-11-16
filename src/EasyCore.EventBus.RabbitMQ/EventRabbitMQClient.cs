using EasyCore.EventBus.Event;
using EasyCore.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EasyCore.EventBus.RabbitMQ
{
    /// <summary>
    /// EventBus adapter that publishes and consumes distributed events over <see cref="IRabbitMQClient"/>.
    /// Maps event type names to RabbitMQ routing keys and acknowledges or nacks deliveries based on dispatch results.
    /// </summary>
    public sealed class EventRabbitMQClient : IEventMessageQueueClient
    {
        /// <summary>
        /// Underlying RabbitMQ client used for connect, publish, subscribe, ack, and nack.
        /// </summary>
        private readonly IRabbitMQClient _client;

        /// <summary>
        /// Dispatches deserialized events to registered handlers with retry semantics.
        /// </summary>
        private readonly DistributedEventDispatcher _dispatcher;

        /// <summary>
        /// EventBus options providing default retry count and interval for published messages.
        /// </summary>
        private readonly EventBusOptions _eventBusOptions;

        /// <summary>
        /// Logger for subscription and dispatch diagnostics.
        /// </summary>
        private readonly ILogger<EventRabbitMQClient> _logger;

        /// <summary>
        /// Maps distributed event type names (routing keys) to CLR types discovered at subscribe time.
        /// </summary>
        private IReadOnlyDictionary<string, Type> _eventTypes = new Dictionary<string, Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventRabbitMQClient"/> class.
        /// </summary>
        /// <param name="client">The RabbitMQ infrastructure client.</param>
        /// <param name="dispatcher">The distributed event dispatcher.</param>
        /// <param name="eventBusOptions">EventBus configuration options.</param>
        /// <param name="logger">Optional logger; uses a null logger when omitted.</param>
        public EventRabbitMQClient(
            IRabbitMQClient client,
            DistributedEventDispatcher dispatcher,
            IOptions<EventBusOptions> eventBusOptions,
            ILogger<EventRabbitMQClient>? logger = null)
        {
            _client = client;
            _dispatcher = dispatcher;
            _eventBusOptions = eventBusOptions.Value;
            _logger = logger ?? NullLogger<EventRabbitMQClient>.Instance;
        }

        /// <summary>
        /// Establishes the connection to the RabbitMQ broker.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the connect operation.</param>
        /// <returns>A task that completes when the connection is established.</returns>
        public Task ConnectAsync(CancellationToken cancellationToken = default) =>
            _client.ConnectAsync(cancellationToken);

        /// <summary>
        /// Scans for distributed event types and subscribes to their RabbitMQ routing keys.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the subscribe operation.</param>
        /// <returns>A task that completes when subscriptions are registered.</returns>
        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            _eventTypes = EventTypeScanner.GetDistributedEventTypes().ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);
            var routingKeys = _eventTypes.Keys.ToList();

            await _client.SubscribeAsync(routingKeys, OnMessageAsync, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Publishes an event synchronously by blocking on <see cref="PublishAsync{TEvent}"/>.
        /// </summary>
        /// <typeparam name="TEvent">The event type implementing <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <returns><c>true</c> when the publish completes successfully.</returns>
        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent =>
            PublishAsync(eventMessage).GetAwaiter().GetResult();

        /// <summary>
        /// Serializes and publishes an event to RabbitMQ using the event type name as the routing key.
        /// </summary>
        /// <typeparam name="TEvent">The event type implementing <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
        /// <returns><c>true</c> when the message is published successfully.</returns>
        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken = default)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(eventMessage);

            var routingKey = eventMessage.GetType().Name;
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventMessage, eventMessage.GetType()));
            var headers = new Dictionary<string, object>
            {
                [EventMessageHeaders.EventType] = routingKey,
                [EventMessageHeaders.Retry] = _eventBusOptions.RetryCount,
                [EventMessageHeaders.RetryInterval] = _eventBusOptions.RetryInterval
            };

            await _client.PublishAsync(routingKey, body, headers, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Handles a delivered RabbitMQ message: resolves the event type, dispatches it, then acks or nacks.
        /// </summary>
        /// <param name="message">The delivered RabbitMQ message.</param>
        /// <param name="cancellationToken">Token used to cancel dispatch.</param>
        /// <returns>A task that completes when the message has been processed.</returns>
        private async Task OnMessageAsync(RabbitMQDeliveredMessage message, CancellationToken cancellationToken)
        {
            if (!_eventTypes.TryGetValue(message.RoutingKey, out var eventType))
            {
                _logger.LogWarning("Unknown RabbitMQ routing key {RoutingKey}, nacking with requeue.", message.RoutingKey);
                _client.Nack(message.DeliveryTag, requeue: true);
                return;
            }

            var payload = Encoding.UTF8.GetString(message.Body.Span);
            var (maxRetry, interval) = DistributedEventDispatcher.ParseRetryHeaders(message.Headers, _eventBusOptions);

            var result = await _dispatcher.DispatchAsync(eventType, payload, maxRetry, interval, cancellationToken)
                .ConfigureAwait(false);

            if (result is EventDispatchResult.Handled or EventDispatchResult.RetryExhausted or EventDispatchResult.NoHandler)
                _client.Ack(message.DeliveryTag);
            else
                _client.Nack(message.DeliveryTag, requeue: true);
        }

        /// <summary>
        /// Disposes the underlying RabbitMQ client.
        /// </summary>
        /// <returns>A value task that completes when disposal finishes.</returns>
        public ValueTask DisposeAsync() => _client.DisposeAsync();
    }
}
