using EasyCore.EventBus.Event;
using EasyCore.EventBus.RabbitMQ.Exchange;

namespace EasyCore.EventBus.RabbitMQ
{
    public class EventRabbitMQClient : IEventMessageQueueClient
    {
        private readonly IRabbitMQExchangecs _toipcExchange;

        public EventRabbitMQClient(IRabbitMQExchangecs toipcExchange) => _toipcExchange = toipcExchange;

        public void Connect() => _toipcExchange.Connect();

        public void Subscribe() => _toipcExchange.Subscribe();

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent => _toipcExchange.Publish(eventMessage);

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent => await _toipcExchange.PublishAsync(eventMessage);
    }
}
