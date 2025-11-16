using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Local
{
    /// <summary>
    /// Abstraction for publishing events within the same process.
    /// </summary>
    public interface ILocalEventBus
    {
        /// <summary>
        /// Publishes an event asynchronously to all registered local handlers.
        /// </summary>
        /// <typeparam name="TEvent">The event type, which must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <returns>A task that completes when all handlers have finished processing.</returns>
        Task PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent;
    }
}
