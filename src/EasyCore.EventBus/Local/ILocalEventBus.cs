using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Local
{
    public interface ILocalEventBus
    {
        /// <summary>
        /// Event Bus - Event Publishing Interface.
        /// </summary>
        /// <typeparam name="TEvent">Event Object</typeparam>
        /// <param name="eventMessage">Event Message</param>
        /// <returns></returns>
        Task PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent;
    }
}
