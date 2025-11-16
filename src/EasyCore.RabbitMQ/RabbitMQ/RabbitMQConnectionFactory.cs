using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EasyCore.RabbitMQ
{
    /// <summary>
    /// Creates RabbitMQ connections and channels from <see cref="RabbitMQOptions"/>.
    /// </summary>
    public class RabbitMQConnectionFactory
    {
        /// <summary>
        /// Configured RabbitMQ connection options.
        /// </summary>
        private readonly RabbitMQOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQConnectionFactory"/> class.
        /// </summary>
        /// <param name="options">RabbitMQ options accessor.</param>
        public RabbitMQConnectionFactory(IOptions<RabbitMQOptions> options) => _options = options.Value;

        /// <summary>
        /// Creates a new RabbitMQ connection using the configured host(s) and credentials.
        /// </summary>
        /// <returns>An open <see cref="IConnection"/> instance.</returns>
        public IConnection CreateConnection()
        {
            var factory = new ConnectionFactory
            {
                UserName = _options.UserName,
                Password = _options.Password,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                DispatchConsumersAsync = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                ContinuationTimeout = TimeSpan.FromSeconds(30),
                SocketReadTimeout = TimeSpan.FromSeconds(30),
                SocketWriteTimeout = TimeSpan.FromSeconds(30)
            };

            if (_options.HostName.Contains(','))
                return factory.CreateConnection(AmqpTcpEndpoint.ParseMultiple(_options.HostName));

            factory.HostName = _options.HostName;
            return factory.CreateConnection();
        }

        /// <summary>
        /// Creates a new channel (model) on the specified connection.
        /// </summary>
        /// <param name="connection">Open RabbitMQ connection.</param>
        /// <returns>A new <see cref="IModel"/> channel.</returns>
        public IModel CreateModel(IConnection connection) => connection.CreateModel();
    }
}
