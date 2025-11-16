namespace EasyCore.EventBus.RabbitMQ
{
    /// <summary>
    /// Extension methods that register the RabbitMQ EventBus adapter on <see cref="EventBusOptions"/>.
    /// </summary>
    public static class EventBusRabbitMQExtensions
    {
        /// <summary>
        /// Configures EventBus to use RabbitMQ with the specified broker host name.
        /// </summary>
        /// <param name="options">The EventBus options being configured.</param>
        /// <param name="hostName">RabbitMQ broker host name; must be non-empty.</param>
        /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="hostName"/> is null or empty.</exception>
        public static EventBusOptions RabbitMQ(this EventBusOptions options, string hostName)
        {
            if (string.IsNullOrEmpty(hostName))
                throw new ArgumentException("Host name is required.", nameof(hostName));

            return options.RabbitMQ(opt => opt.HostName = hostName);
        }

        /// <summary>
        /// Configures EventBus to use RabbitMQ with a custom options callback.
        /// </summary>
        /// <param name="options">The EventBus options being configured.</param>
        /// <param name="configure">Action that configures <see cref="RabbitMQOptions"/>.</param>
        /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
        public static EventBusOptions RabbitMQ(this EventBusOptions options, Action<RabbitMQOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            options.RegisterExtension(new RabbitMQOptionsExtension(configure));
            return options;
        }
    }
}
