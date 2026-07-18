using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.Kafka
{
    /// <summary>
    /// DI registration for EasyCore.Kafka.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEasyCoreKafka(this IServiceCollection services, Action<KafkaOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.TryAddSingleton<IKafkaClient, KafkaClient>();
            return services;
        }
    }
}