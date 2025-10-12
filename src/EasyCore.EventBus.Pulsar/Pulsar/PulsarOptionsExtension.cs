using EasyCore.EventBus.Event;
using EasyCore.EventBus.Pulsar.Exchange;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.EventBus.Pulsar
{
    public class PulsarOptionsExtension : IEventOptionsExtension
    {
        private readonly Action<PulsarOptions> _configure;

        public PulsarOptionsExtension(Action<PulsarOptions> configure) => _configure = configure;

        public void AddServices(IServiceCollection services)
        {
            services.Configure(_configure);

            services.AddSingleton<IPulsarExchangecs, PulsarExchangecs>();

            services.AddSingleton<IConnectionChannel, ConnectionChannel>();

            services.TryAddSingleton<IEventMessageQueueClient, EventPulsarClient>();
        }
    }
}
