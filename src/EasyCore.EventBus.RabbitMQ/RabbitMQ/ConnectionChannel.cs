using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EasyCore.EventBus.RabbitMQ
{
    public class ConnectionChannel : IConnectionChannel, IDisposable
    {
        private IConnection? _connection;
        private readonly RabbitMQOptions _options;

        public ConnectionChannel(IOptions<RabbitMQOptions> options)
        {
            _options = options.Value;
        }

        public void Dispose()
        {
            _connection!.Dispose();
        }

        public IConnection GetConnection()
        {
            _connection?.Close();
            _connection?.Dispose();
            _connection = CreateConnection();
            return _connection;
        }

        public IConnection CreateConnection()
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

            if (_options.HostName.Contains(",")) return _connection = factory.CreateConnection(AmqpTcpEndpoint.ParseMultiple(_options.HostName));

            factory.HostName = _options.HostName;

            return _connection = factory.CreateConnection();
        }

        public IModel CreateModel()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("Connection is null");
            }
            return _connection.CreateModel();
        }
    }
}
