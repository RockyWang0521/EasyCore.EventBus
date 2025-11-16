using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;

namespace EasyCore.RabbitMQ
{
    /// <summary>
    /// Default <see cref="IRabbitMQClient"/> implementation.
    /// </summary>
    public sealed class RabbitMQClient : IRabbitMQClient
    {
        private readonly RabbitMQConnectionFactory _connectionFactory;
        private readonly RabbitMQOptions _options;
        private readonly ILogger<RabbitMQClient> _logger;
        private readonly object _sync = new();

        private IConnection? _connection;
        private IModel? _channel;
        private AsyncEventingBasicConsumer? _consumer;
        private Func<RabbitMQDeliveredMessage, CancellationToken, Task>? _handler;
        private CancellationTokenSource? _subscribeCts;
        private bool _confirmsEnabled;
        private bool _disposed;

        public RabbitMQClient(
            RabbitMQConnectionFactory connectionFactory,
            IOptions<RabbitMQOptions> options,
            ILogger<RabbitMQClient>? logger = null)
        {
            _connectionFactory = connectionFactory;
            _options = options.Value;
            _logger = logger ?? NullLogger<RabbitMQClient>.Instance;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureChannel();
            return Task.CompletedTask;
        }

        public Task PublishAsync(
            string routingKey,
            ReadOnlyMemory<byte> body,
            IDictionary<string, object>? headers = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrEmpty(routingKey);

            EnsureChannel();

            var props = _channel!.CreateBasicProperties();
            props.DeliveryMode = 2;
            if (headers != null)
                props.Headers = new Dictionary<string, object>(headers);

            if (!_confirmsEnabled)
            {
                _channel.ConfirmSelect();
                _confirmsEnabled = true;
            }

            _channel.BasicPublish(_options.ExchangeName, routingKey, props, body.ToArray());
            _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(
            IEnumerable<string> routingKeys,
            Func<RabbitMQDeliveredMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(handler);
            EnsureChannel();

            var keys = routingKeys.Distinct(StringComparer.Ordinal).ToList();
            if (keys.Count == 0)
            {
                _logger.LogWarning("RabbitMQ SubscribeAsync called with no routing keys.");
                return Task.CompletedTask;
            }

            _handler = handler;
            _subscribeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var appName = _options.AppName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "EasyCore";
            var queueName = $"{appName}.{_options.QueueName}";

            var arguments = new Dictionary<string, object> { { "x-message-ttl", _options.MessageTTL } };
            if (!string.IsNullOrEmpty(_options.QueueMode))
                arguments["x-queue-mode"] = _options.QueueMode!;
            if (!string.IsNullOrEmpty(_options.QueueType))
                arguments["x-queue-type"] = _options.QueueType!;

            _channel!.QueueDeclare(queueName, _options.Durable, _options.Exclusive, _options.AutoDelete, arguments);

            foreach (var routingKey in keys)
                _channel.QueueBind(queueName, _options.ExchangeName, routingKey);

            _channel.BasicQos(0, 1, false);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += OnReceivedAsync;
            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: _consumer);

            _logger.LogInformation("RabbitMQ subscribed to queue {Queue} with {Count} bindings.", queueName, keys.Count);
            return Task.CompletedTask;
        }

        public void Ack(ulong deliveryTag) => _channel?.BasicAck(deliveryTag, false);

        public void Nack(ulong deliveryTag, bool requeue) => _channel?.BasicNack(deliveryTag, false, requeue);

        private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs e)
        {
            var ct = _subscribeCts?.Token ?? CancellationToken.None;
            if (ct.IsCancellationRequested)
                return;

            IReadOnlyDictionary<string, object>? headers = null;
            if (e.BasicProperties?.Headers != null)
                headers = new Dictionary<string, object>(e.BasicProperties.Headers);

            var message = new RabbitMQDeliveredMessage
            {
                RoutingKey = e.RoutingKey,
                Body = e.Body.ToArray(),
                Headers = headers,
                DeliveryTag = e.DeliveryTag,
                CorrelationId = e.BasicProperties?.CorrelationId
            };

            try
            {
                if (_handler != null)
                    await _handler(message, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in RabbitMQ message handler for {RoutingKey}.", e.RoutingKey);
                throw;
            }
        }

        private void EnsureChannel()
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_channel is { IsOpen: true })
                    return;

                _connection?.Dispose();
                _connection = _connectionFactory.CreateConnection();
                _channel = _connectionFactory.CreateModel(_connection);
                _confirmsEnabled = false;
                _channel.ExchangeDeclare(_options.ExchangeName, _options.ExchangeType, durable: true);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            _subscribeCts?.Cancel();
            _subscribeCts?.Dispose();

            if (_consumer != null)
                _consumer.Received -= OnReceivedAsync;

            try
            {
                _channel?.Close();
                _channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing RabbitMQ channel.");
            }

            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing RabbitMQ connection.");
            }

            await Task.CompletedTask;
        }
    }
}
