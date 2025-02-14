using System;
using System.Collections.Generic;
using System.Text;

namespace EasyCore.EventBus.RabbitMQ
{
    public static class EventBusRabbitMQExtensions
    {
        public static EventBusOptions RabbitMQ(this EventBusOptions options, Action<RabbitMQOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var rabbitMQOptions = new RabbitMQOptions();

            configure.Invoke(rabbitMQOptions);

            options.RegisterExtension(new RabbitMQOptionsExtension(configure));

            options = options.RabbitMQ(rabbitMQOptions);

            return options;
        }

        public static EventBusOptions RabbitMQ(this EventBusOptions options, RabbitMQOptions rabbitMQOptions)
        {
            options.ExchangeName = rabbitMQOptions.ExchangeName;
            options.HostName = rabbitMQOptions.HostName;
            options.Password = rabbitMQOptions.Password;
            options.UserName = rabbitMQOptions.UserName;
            options.Port = rabbitMQOptions.Port;

            return options;
        }
    }
}
