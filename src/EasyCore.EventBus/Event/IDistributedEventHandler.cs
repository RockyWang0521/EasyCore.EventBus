namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Marker interface for handlers that process distributed (cross-process) events.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle, which must implement <see cref="IEvent"/>.</typeparam>
    public interface IDistributedEventHandler<TEvent> : IEventHandler<TEvent> where TEvent : IEvent
    {

    }
}
