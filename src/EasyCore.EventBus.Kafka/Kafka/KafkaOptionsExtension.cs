using EasyCore.EventBus.Event;
using EasyCore.EventBus.Kafka.Exchange;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.EventBus.Kafka.Kafka
{
    public class KafkaOptionsExtension : IEventOptionsExtension
    {
        private readonly Action<KafkaOptions> _configure;

        public KafkaOptionsExtension(Action<KafkaOptions> configure)=>_configure = configure;
        
        public void AddServices(IServiceCollection services)
        {
            services.Configure(_configure);

            services.AddSingleton<IKafkaExchangecs, KafkaExchangecs>();

            services.AddSingleton<IConnectionChannel, ConnectionChannel>();

            services.TryAddSingleton<IEventMessageQueueClient, EventKafkaClient>();
        }
    }
}
