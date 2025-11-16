namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Contract for handling a specific event type asynchronously.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle, which must implement <see cref="IEvent"/>.</typeparam>
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// Handles the given event message.
        /// </summary>
        /// <param name="eventMessage">The event instance to process.</param>
        /// <returns>A task that represents the asynchronous handling operation.</returns>
        Task HandleAsync(TEvent eventMessage);
    }
}
