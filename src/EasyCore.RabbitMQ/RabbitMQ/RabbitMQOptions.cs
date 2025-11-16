namespace EasyCore.RabbitMQ
{
    /// <summary>
    /// RabbitMQ connection and topology options used by <see cref="RabbitMQClient"/>.
    /// </summary>
    public class RabbitMQOptions
    {
        /// <summary>
        /// Gets or sets the host name. Multiple hosts may be comma-separated for cluster endpoints.
        /// </summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the RabbitMQ user name.
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// Gets or sets the RabbitMQ password.
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Gets or sets the AMQP port.
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// Gets or sets the exchange name used for publish and subscribe.
        /// </summary>
        public string ExchangeName { get; set; } = "EasyCore.EventBus";

        /// <summary>
        /// Gets or sets the queue name suffix appended after the application name.
        /// </summary>
        public string QueueName { get; set; } = "EasyCore.Queue";

        /// <summary>
        /// Gets or sets the exchange type (for example topic, direct, fanout, or headers).
        /// </summary>
        public string ExchangeType { get; set; } = "topic";

        /// <summary>
        /// Gets or sets the virtual host.
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Gets or sets the queue message TTL in milliseconds. Default is 10 days.
        /// </summary>
        public int MessageTTL { get; set; } = 864000000;

        /// <summary>
        /// Gets or sets an optional queue mode (for example <c>lazy</c>).
        /// </summary>
        public string? QueueMode { get; set; }

        /// <summary>
        /// Gets or sets whether the queue is durable.
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the queue is exclusive to a single connection.
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the queue is auto-deleted when unused.
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Gets or sets an optional queue type (for example <c>quorum</c>).
        /// </summary>
        public string? QueueType { get; set; }

        /// <summary>
        /// Gets or sets the application name prefix used for queue naming. When null, the entry assembly name is used.
        /// </summary>
        public string? AppName { get; set; }
    }
}
