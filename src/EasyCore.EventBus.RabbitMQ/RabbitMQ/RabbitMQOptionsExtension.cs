using EasyCore.EventBus.Event;
using EasyCore.EventBus.RabbitMQ.Exchange.Interfaces;
using EasyCore.EventBus.RabbitMQ.Exchange.Servers;
using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.EventBus.RabbitMQ
{
    public class RabbitMQOptionsExtension : IEventOptionsExtension
    {
        private readonly Action<RabbitMQOptions> _configure;

        public  RabbitMQOptionsExtension(Action<RabbitMQOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.Configure(_configure);

            services.AddSingleton<IToipcExchangecs, ToipcExchangecs>();
            services.AddSingleton<IConnectionChannel, ConnectionChannel>();
            services.AddSingleton<IEventRabbitMQClient, EventRabbitMQClient>();
        }
    }
}
