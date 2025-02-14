namespace EasyCore.EventBus.Event
{
    public interface IEventRabbitMQClient
    {
        /// <summary>
        /// 创建RabbitMQ连接
        /// </summary>
        void Create();

        /// <summary>
        /// 订阅RabbitMQ
        /// </summary>
        void Subscribe();

        /// <summary>
        /// 事件总现--事件发布接口
        /// </summary>
        /// <typeparam name="TEvent">事件对象</typeparam>
        /// <param name="eventMessage">事件消息</param>
        /// <returns></returns>
        Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent;

        /// <summary>
        /// 事件总现--事件发布接口
        /// </summary>
        /// <typeparam name="TEvent">事件对象</typeparam>
        /// <param name="eventMessage">事件消息</param>
        /// <returns></returns>
        bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent;
    }
}
