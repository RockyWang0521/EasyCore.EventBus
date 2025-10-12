namespace EasyCore.EventBus.RabbitMQ
{
    public static class EventBusRabbitMQExtensions
    {
        public static EventBusOptions RabbitMQ(this EventBusOptions options, string hostName)
        {
            if (string.IsNullOrEmpty(hostName)) throw new ArgumentException(nameof(hostName));

            var configure = new Action<RabbitMQOptions>(options =>
            {
                options.HostName = hostName;
            });

            options.RegisterExtension(new RabbitMQOptionsExtension(configure));

            return options;
        }

        public static EventBusOptions RabbitMQ(this EventBusOptions options, Action<RabbitMQOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var rabbitMQOptions = new RabbitMQOptions();

            configure.Invoke(rabbitMQOptions);

            options.RegisterExtension(new RabbitMQOptionsExtension(configure));

            return options;
        }
    }
}
