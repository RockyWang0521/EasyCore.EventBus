namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Distributed Event Handling Interface Definition
    /// </summary>
    /// <typeparam name="TEvent">Event Object</typeparam>
    public interface IDistributedEventHandler<TEvent> : IEventHandler<TEvent> where TEvent : IEvent
    {

    }
}
