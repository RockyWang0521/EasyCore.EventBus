using EasyCore.EventBus.Event;
using EasyCore.EventBus.RedisStreams.Exchange;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.EventBus.RedisStreams
{
    public class RedisStreamsOptionsExtension : IEventOptionsExtension
    {
        private readonly Action<RedisStreamsOptions> _configure;

        public RedisStreamsOptionsExtension(Action<RedisStreamsOptions> configure) => _configure = configure;

        public void AddServices(IServiceCollection services)
        {
            services.Configure(_configure);

            services.AddSingleton<IRedisStreamsExchangecs, RedisStreamsExchangecs>();

            services.AddSingleton<IConnectionChannel, ConnectionChannel>();

            services.TryAddSingleton<IEventMessageQueueClient, EventRedisStreamsClient>();
        }
    }
}
