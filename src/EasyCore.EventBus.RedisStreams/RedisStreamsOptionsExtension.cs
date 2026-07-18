using EasyCore.EventBus.Event;
using EasyCore.RedisStreams;
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
            services.AddEasyCoreRedisStreams(_configure);
            services.TryAddSingleton<IEventMessageQueueClient, EventRedisStreamsClient>();
        }
    }
}
