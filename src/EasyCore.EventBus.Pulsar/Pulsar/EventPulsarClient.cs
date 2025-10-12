using EasyCore.EventBus.Event;
using EasyCore.EventBus.Pulsar.Exchange;

namespace EasyCore.EventBus.Pulsar
{
    public class EventPulsarClient : IEventMessageQueueClient
    {
        private readonly IPulsarExchangecs _pulsarExchange;

        public EventPulsarClient(IPulsarExchangecs pulsarExchange) => _pulsarExchange = pulsarExchange;

        public void Connect() => _pulsarExchange.Connect();

        public void Subscribe() => _pulsarExchange.Subscribe();

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent => _pulsarExchange.Publish(eventMessage);

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent => await _pulsarExchange.PublishAsync(eventMessage);
    }
}
