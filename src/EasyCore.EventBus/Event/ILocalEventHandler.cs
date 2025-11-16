namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Marker interface for handlers that process local (in-process) events.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle, which must implement <see cref="IEvent"/>.</typeparam>
    public interface ILocalEventHandler<TEvent> : IEventHandler<TEvent> where TEvent : IEvent
    {

    }
}
