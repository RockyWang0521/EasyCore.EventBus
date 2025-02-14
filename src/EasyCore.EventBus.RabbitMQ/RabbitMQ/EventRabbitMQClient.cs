using EasyCore.EventBus.Event;
using EasyCore.EventBus.RabbitMQ.Exchange.Interfaces;

namespace EasyCore.EventBus.RabbitMQ
{
    public class EventRabbitMQClient : IEventRabbitMQClient
    {
        private readonly IToipcExchangecs _toipcExchange;

        public EventRabbitMQClient(IToipcExchangecs toipcExchange)
        {
            _toipcExchange = toipcExchange;
        }
        public void Create()
        {
            _toipcExchange.Connect();
        }

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            return _toipcExchange.Publish(eventMessage);
        }

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            return await _toipcExchange.PublishAsync(eventMessage);
        }

        public void Subscribe()
        {
            _toipcExchange.Subscribe();
        }
    }
}
