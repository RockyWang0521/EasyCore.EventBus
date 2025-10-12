namespace EasyCore.EventBus.RabbitMQ
{
    public class RabbitMQOptions
    {
        /// <summary>
        /// RabbitMQ HostName
        /// </summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// RabbitMQ UserName
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// RabbitMQ Password
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// RabbitMQ Port
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// RabbitMQ ExchangeName
        /// </summary>
        public string ExchangeName { get; set; } = "EasyCore.EventBus";

        /// <summary>
        /// RabbitMQ QueueName
        /// </summary>
        public string QueueName { get; set; } = "EasyCore.Queue";

        /// <summary>
        /// RabbitMQ ExchangeType
        /// </summary>
        public string ExchangeType { get; set; } = "topic";

        /// <summary>
        /// RabbitMQ VirtualHost
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Gets or sets the queue message auto-delete time, default is 10 days (in milliseconds).
        /// </summary>
        public int MessageTTL { get; set; } = 864000000;

        /// <summary>
        ///  RabbitMQ QueueMode
        /// </summary>
        public string QueueMode { get; set; } = default!;

        /// <summary>
        /// RabbitMQ Durable
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// RabbitMQ Exclusive
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// RabbitMQ AutoDelete
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// RabbitMQ QueueType
        /// </summary>
        public string QueueType { get; set; } = default!;
    }
}
