namespace EasyCore.EventBus.Kafka.Exchange
{
    public interface IKafkaExchangecs : IDisposable
    {
        /// <summary>
        /// Connect to the Kafka server.
        /// </summary>
        void Connect();

        /// <summary>
        /// Subscribe to the Kafka topic.
        /// </summary>
        void Subscribe();

        /// <summary>
        /// Publish the event message to the Kafka topic.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        bool Publish<TEvent>(TEvent eventMessage);

        /// <summary>
        /// Publish the event message to the Kafka topic asynchronously.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        Task<bool> PublishAsync<TEvent>(TEvent eventMessage);
    }
}
