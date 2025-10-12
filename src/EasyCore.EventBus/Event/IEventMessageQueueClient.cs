namespace EasyCore.EventBus.Event
{
    public interface IEventMessageQueueClient
    {
        /// <summary>
        /// Connect to the message queue server.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnect from the message queue server.
        /// </summary>
        void Subscribe();

        /// <summary>
        /// Publish an event message to the message queue server.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent;

        /// <summary>
        /// Publish an event message to the message queue server asynchronously.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent;
    }
}
