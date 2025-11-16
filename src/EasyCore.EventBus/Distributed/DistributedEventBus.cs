using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Distributed
{
    /// <summary>
    /// Distributed event bus that delegates publish operations to an <see cref="IEventMessageQueueClient"/>.
    /// </summary>
    public class DistributedEventBus : IDistributedEventBus
    {
        /// <summary>
        /// The transport client that performs actual publish operations.
        /// </summary>
        private readonly IEventMessageQueueClient _messageQueueClient;

        /// <summary>
        /// Initializes a new instance of <see cref="DistributedEventBus"/>.
        /// </summary>
        /// <param name="messageQueueClient">The message queue client used for publishing.</param>
        public DistributedEventBus(IEventMessageQueueClient messageQueueClient) =>
            _messageQueueClient = messageQueueClient;

        /// <summary>
        /// Publishes an event message synchronously to the distributed transport.
        /// </summary>
        /// <typeparam name="TEvent">The event type, which must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <returns><c>true</c> if the publish succeeded; otherwise, <c>false</c>.</returns>
        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent =>
            _messageQueueClient.Publish(eventMessage);

        /// <summary>
        /// Publishes an event message asynchronously to the distributed transport.
        /// </summary>
        /// <typeparam name="TEvent">The event type, which must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <returns>
        /// A task that resolves to <c>true</c> if the publish succeeded; otherwise, <c>false</c>.
        /// </returns>
        public Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent =>
            _messageQueueClient.PublishAsync(eventMessage);
    }
}
