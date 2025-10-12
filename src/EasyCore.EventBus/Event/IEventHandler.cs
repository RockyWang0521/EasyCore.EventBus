namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Event Execution Interface
    /// </summary>
    /// <typeparam name="TEvent">Event Object</typeparam>
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent eventMessage);
    }
}
