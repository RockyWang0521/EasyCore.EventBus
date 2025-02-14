using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.EventBus.Event
{
    public interface IEventOptionsExtension
    {
        void AddServices(IServiceCollection services);
    }
}
