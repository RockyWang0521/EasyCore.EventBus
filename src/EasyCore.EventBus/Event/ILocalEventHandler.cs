namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Local Event Handling Interface Definition
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface ILocalEventHandler<TEvent>  : IEventHandler<TEvent> where TEvent : IEvent
    {

    }
}
