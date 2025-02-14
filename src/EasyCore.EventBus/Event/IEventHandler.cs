namespace EasyCore.EventBus.Event
{
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent eventMessage);
    }
}
