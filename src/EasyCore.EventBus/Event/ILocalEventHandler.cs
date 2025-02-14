namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// 事件处理接口定义
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface ILocalEventHandler<TEvent>  : IEventHandler<TEvent> where TEvent : IEvent
    {

    }
}
