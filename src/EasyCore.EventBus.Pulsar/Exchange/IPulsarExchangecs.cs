using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Pulsar.Exchange
{
    public interface IPulsarExchangecs : IDisposable
    {
        /// <summary>
        /// Connect to the Pulsar server.
        /// </summary>
        Task Connect();

        /// <summary>
        /// Subscribe to the Pulsar Topic.
        /// </summary>
        Task Subscribe();

        /// <summary>
        /// Publish the event message to the Pulsar Topic.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent;

        /// <summary>
        /// Publish the event message to the Pulsar Topic.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent;
    }
}
