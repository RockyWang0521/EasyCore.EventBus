using EasyCore.EventBus.Event;
using EasyCore.EventBus.RabbitMQ.Exchange;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.EventBus.RabbitMQ
{
    public class RabbitMQOptionsExtension : IEventOptionsExtension
    {
        private readonly Action<RabbitMQOptions> _configure;

        public RabbitMQOptionsExtension(Action<RabbitMQOptions> configure) => _configure = configure;

        public void AddServices(IServiceCollection services)
        {
            services.Configure(_configure);

            services.AddSingleton<IRabbitMQExchangecs, RabbitMQExchangecs>();

            services.AddSingleton<IConnectionChannel, ConnectionChannel>();

            services.TryAddSingleton<IEventMessageQueueClient, EventRabbitMQClient>();
        }
    }
}
