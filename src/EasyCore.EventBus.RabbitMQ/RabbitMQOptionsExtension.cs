using EasyCore.EventBus.Event;
using EasyCore.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyCore.EventBus.RabbitMQ
{
    /// <summary>
    /// Registers RabbitMQ infrastructure and the EventBus RabbitMQ client with the DI container.
    /// </summary>
    public class RabbitMQOptionsExtension : IEventOptionsExtension
    {
        /// <summary>
        /// Delegate that configures <see cref="RabbitMQOptions"/> when services are added.
        /// </summary>
        private readonly Action<RabbitMQOptions> _configure;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQOptionsExtension"/> class.
        /// </summary>
        /// <param name="configure">Action used to configure RabbitMQ options.</param>
        public RabbitMQOptionsExtension(Action<RabbitMQOptions> configure) => _configure = configure;

        /// <summary>
        /// Adds EasyCore RabbitMQ services and registers <see cref="EventRabbitMQClient"/> as <see cref="IEventMessageQueueClient"/>.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public void AddServices(IServiceCollection services)
        {
            services.AddEasyCoreRabbitMQ(_configure);
            services.TryAddSingleton<IEventMessageQueueClient, EventRabbitMQClient>();
        }
    }
}
