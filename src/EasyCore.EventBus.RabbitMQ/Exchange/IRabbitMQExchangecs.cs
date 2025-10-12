using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.RabbitMQ.Exchange
{
    public interface IRabbitMQExchangecs : IDisposable
    {
        /// <summary>
        /// Connect to the RabbitMQ server.
        /// </summary>
        void Connect();

        /// <summary>
        /// Subscribe to the RabbitMQ Queues.
        /// </summary>
        void Subscribe();

        /// <summary>
        /// Publish the event message to the RabbitMQ RoutingKey.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent;

        /// <summary>
        /// Publish the event message to the RabbitMQ RoutingKey.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent;
    }
}
