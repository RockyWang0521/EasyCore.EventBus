using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.RabbitMQ
{
    /// <summary>
    /// Extension methods for registering EasyCore.RabbitMQ services with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers RabbitMQ options, <see cref="RabbitMQConnectionFactory"/>, and <see cref="IRabbitMQClient"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configure">Callback used to configure <see cref="RabbitMQOptions"/>.</param>
        /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection EasyCoreRabbitMQ(this IServiceCollection services, Action<RabbitMQOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.TryAddSingleton<RabbitMQConnectionFactory>();
            services.TryAddSingleton<IRabbitMQClient, RabbitMQClient>();
            return services;
        }
    }
}
