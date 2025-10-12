using EasyCore.EventBus.Event;
using EasyCore.EventBus.Kafka.Exchange;

namespace EasyCore.EventBus.Kafka.Kafka
{
    public class EventKafkaClient : IEventMessageQueueClient
    {
        private readonly IKafkaExchangecs _kafkaExchange;

        public EventKafkaClient(IKafkaExchangecs kafkaExchange) => _kafkaExchange = kafkaExchange;

        public void Connect() => _kafkaExchange.Connect();

        public void Subscribe() => _kafkaExchange.Subscribe();

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent => _kafkaExchange.Publish(eventMessage);

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent => await _kafkaExchange.PublishAsync(eventMessage);
    }
}
