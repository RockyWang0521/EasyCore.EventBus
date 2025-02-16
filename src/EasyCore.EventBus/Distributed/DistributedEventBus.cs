using EasyCore.EventBus.Event;
using Microsoft.Extensions.Options;

namespace EasyCore.EventBus.Distributed
{
    public class DistributedEventBus : IDistributedEventBus
    {
        private readonly IEventRabbitMQClient _eventRabbitMQClient;

        public DistributedEventBus(IEventRabbitMQClient eventRabbitMQClient)
        {
            _eventRabbitMQClient = eventRabbitMQClient;
        }

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            return _eventRabbitMQClient.Publish(eventMessage);
        }

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            return await _eventRabbitMQClient.PublishAsync(eventMessage);
        }
    }
}
