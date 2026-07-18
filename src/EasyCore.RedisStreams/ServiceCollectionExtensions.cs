using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.RedisStreams
{
    /// <summary>
    /// DI registration for EasyCore.RedisStreams.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEasyCoreRedisStreams(this IServiceCollection services, Action<RedisStreamsOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.TryAddSingleton<IRedisStreamsClient, RedisStreamsClient>();
            return services;
        }
    }
}