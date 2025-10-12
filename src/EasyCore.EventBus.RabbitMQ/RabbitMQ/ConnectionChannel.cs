using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EasyCore.EventBus.RabbitMQ
{
    public class ConnectionChannel : IConnectionChannel
    {
        private readonly RabbitMQOptions _options;

        public ConnectionChannel(IOptions<RabbitMQOptions> options) => _options = options.Value;

        public IConnection GetConnection(IConnection? connection)
        {
            if (connection is not null)
            {
                if (connection.IsOpen)
                {
                    connection.Close();

                    connection.Dispose();
                }

                connection = null;
            }

            return CreateConnection(connection);
        }

        public IConnection CreateConnection(IConnection? connection)
        {
            var factory = new ConnectionFactory()
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

            if (_options.HostName.Contains(",")) return connection = factory.CreateConnection(AmqpTcpEndpoint.ParseMultiple(_options.HostName));

            factory.HostName = _options.HostName;

            return connection = factory.CreateConnection();
        }

        public IModel CreateModel(IConnection? connection)
        {
            if (connection == null) connection = CreateConnection(connection);

            return connection.CreateModel();
        }
    }
}
