using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Distributed
{
    public class DistributedEventBus : IDistributedEventBus
    {
        private readonly IEventMessageQueueClient _MessageQueueClient;

        public DistributedEventBus(IEventMessageQueueClient MessageQueueClient) => _MessageQueueClient = MessageQueueClient;

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent => _MessageQueueClient.Publish(eventMessage);

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent => await _MessageQueueClient.PublishAsync(eventMessage);
    }
}
