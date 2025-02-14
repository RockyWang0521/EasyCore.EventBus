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
            if (_connection != null && _connection.IsOpen) return _connection;
            _connection = CreateConnection();
            return _connection;
        }

        public IConnection CreateConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost
            };
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
