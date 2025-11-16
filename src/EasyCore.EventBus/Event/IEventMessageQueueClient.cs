namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Transport adapter used by the distributed event bus for connect, subscribe, and publish.
    /// </summary>
    public interface IEventMessageQueueClient : IAsyncDisposable
    {
        /// <summary>
        /// Connects to the message queue server.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the connect operation.</param>
        /// <returns>A task that completes when the connection is established.</returns>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to events and starts consuming messages.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the subscribe operation.</param>
        /// <returns>A task that completes when subscription setup is finished.</returns>
        Task SubscribeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes an event message synchronously.
        /// </summary>
        /// <typeparam name="TEvent">The event type, which must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <returns><c>true</c> if the publish succeeded; otherwise, <c>false</c>.</returns>
        bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent;

        /// <summary>
        /// Publishes an event message asynchronously.
        /// </summary>
        /// <typeparam name="TEvent">The event type, which must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
        /// <returns>
        /// A task that resolves to <c>true</c> if the publish succeeded; otherwise, <c>false</c>.
        /// </returns>
        Task<bool> PublishAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken = default) where TEvent : IEvent;
    }
}
