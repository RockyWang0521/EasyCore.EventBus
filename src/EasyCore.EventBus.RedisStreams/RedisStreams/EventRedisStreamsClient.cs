using EasyCore.EventBus.Event;
using EasyCore.EventBus.RedisStreams.Exchange;

namespace EasyCore.EventBus.RedisStreams
{
    public class EventRedisStreamsClient : IEventMessageQueueClient
    {
        private readonly IRedisStreamsExchangecs _RedisExchange;

        public EventRedisStreamsClient(IRedisStreamsExchangecs toipcExchange) => _RedisExchange = toipcExchange;

        public void Connect() => _RedisExchange.Connect();

        public void Subscribe() => _RedisExchange.Subscribe();

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent => _RedisExchange.Publish(eventMessage);

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent => await _RedisExchange.PublishAsync(eventMessage);
    }
}
