using EasyCore.EventBus.Event;
using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.EventBus.Local
{
    public class LocalEventBus : ILocalEventBus
    {
        private readonly IServiceProvider _serviceProvider;

        public LocalEventBus(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            var handlers = _serviceProvider.GetServices<ILocalEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                await handler.HandleAsync(eventMessage);
            }
        }
    }
}
