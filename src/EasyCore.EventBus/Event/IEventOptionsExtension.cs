using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Extension contract for registering transport-specific EventBus services.
    /// </summary>
    public interface IEventOptionsExtension
    {
        /// <summary>
        /// Adds the extension's services to the DI container.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        void AddServices(IServiceCollection services);
    }
}
