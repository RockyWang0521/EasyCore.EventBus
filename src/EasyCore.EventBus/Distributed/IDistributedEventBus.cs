using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Distributed
{
    /// <summary>
    /// Abstraction for publishing events to a distributed message transport.
    /// </summary>
    public interface IDistributedEventBus
    {
        /// <summary>
        /// Publishes an event message asynchronously to the distributed transport.
        /// </summary>
        /// <typeparam name="TEvent">The event type, which must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <returns>
        /// A task that resolves to <c>true</c> if the publish succeeded; otherwise, <c>false</c>.
        /// </returns>
        Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent;

        /// <summary>
        /// Publishes an event message synchronously to the distributed transport.
        /// </summary>
        /// <typeparam name="TEvent">The event type, which must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <returns><c>true</c> if the publish succeeded; otherwise, <c>false</c>.</returns>
        bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent;
    }
}
