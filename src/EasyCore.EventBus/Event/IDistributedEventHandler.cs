namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// 分布式事件处理接口定义
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface IDistributedEventHandler<TEvent> : IEventHandler<TEvent> where TEvent : IEvent
    {

    }
}
