using EasyCore.EventBus.Distributed;
using EasyCore.EventBus.Event;
using EasyCore.EventBus.HostedService;
using EasyCore.EventBus.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.EventBus
{
    /// <summary>
    /// DI entry point for EasyCore EventBus registration and configuration.
    /// </summary>
    public static class EasyCoreEventExtend
    {
        /// <summary>
        /// Registers the local event bus and, when configured, distributed transports and handlers.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="action">
        /// Optional configuration for distributed EventBus options and transport extensions.
        /// When provided, registers <see cref="IDistributedEventBus"/> and related hosted services.
        /// </param>
        /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection EasyCoreEventBus(this IServiceCollection services, Action<EventBusOptions>? action = null)
        {
            services.TryAddSingleton<ILocalEventBus, LocalEventBus>();
            services.TryAddSingleton<DistributedEventDispatcher>();

            if (action != null)
            {
                services.TryAddSingleton<IDistributedEventBus, DistributedEventBus>();
                services.AddOptions();

                var options = new EventBusOptions();
                action(options);

                foreach (var serviceExtension in options.Extensions)
                    serviceExtension.AddServices(services);

                if (options.Extensions.Count > 0)
                    services.AddHostedService<EventBusHostedService>();

                services.Configure(action);
            }

            var (localHandlers, distributedHandlers) = EventTypeScanner.GetHandlerRegistrations();

            foreach (var (service, implementation) in localHandlers)
                services.AddTransient(service, implementation);

            foreach (var (service, implementation) in distributedHandlers)
                services.AddTransient(service, implementation);

            return services;
        }
    }
}
