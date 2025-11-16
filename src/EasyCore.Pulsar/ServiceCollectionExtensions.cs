using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.Pulsar
{
    /// <summary>
    /// DI registration for EasyCore.Pulsar.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection EasyCorePulsar(this IServiceCollection services, Action<PulsarOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.TryAddSingleton<IPulsarClient, PulsarClientService>();
            return services;
        }
    }
}
