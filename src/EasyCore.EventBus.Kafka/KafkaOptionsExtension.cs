using EasyCore.EventBus.Event;
using EasyCore.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.EventBus.Kafka
{
    public class KafkaOptionsExtension : IEventOptionsExtension
    {
        private readonly Action<KafkaOptions> _configure;

        public KafkaOptionsExtension(Action<KafkaOptions> configure) => _configure = configure;

        public void AddServices(IServiceCollection services)
        {
            services.EasyCoreKafka(_configure);
            services.TryAddSingleton<IEventMessageQueueClient, EventKafkaClient>();
        }
    }
}
