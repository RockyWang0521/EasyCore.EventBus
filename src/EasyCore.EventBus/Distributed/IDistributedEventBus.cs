using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Distributed
{
    public interface IDistributedEventBus
    {
        /// <summary>
        /// Event Bus - Event Publishing Interface.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent;

        /// <summary>
        /// Event Bus - Event Publishing Interface.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent;
    }
}
