using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Local
{
    public interface ILocalEventBus
    {
        /// <summary>
        /// 事件总现--事件发布接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventMessage"></param>
        /// <returns></returns>
        Task PublishAsync<T>(T eventMessage) where T : IEvent;
    }
}
