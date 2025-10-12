using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Interface for event options extensions.
    /// </summary>
    public interface IEventOptionsExtension
    {
        void AddServices(IServiceCollection services);
    }
}
