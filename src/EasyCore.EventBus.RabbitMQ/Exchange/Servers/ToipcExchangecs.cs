using EasyCore.EventBus.Event;
using EasyCore.EventBus.RabbitMQ.Exchange.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;

namespace EasyCore.EventBus.RabbitMQ.Exchange.Servers
{
    public class ToipcExchangecs : IToipcExchangecs
    {
        private IModel? _channel;
        private string? _appName;
        private IConnection? _connection;
        private List<Type> _routingKeyTypes = new List<Type>();
        private static readonly object Lock = new();
        private AsyncEventingBasicConsumer? _consumer;
        private readonly IConnectionChannel _connectionChannel;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly EventBusOptions _eventBusoptions;

        public ToipcExchangecs(
            IConnectionChannel connectionChannel,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            IOptions<EventBusOptions> eventBusoptions,
            IServiceProvider serviceProvider)
        {
            _connectionChannel = connectionChannel;
            _connection = _connectionChannel!.GetConnection();
            _rabbitMQOptions = rabbitMQOptions.Value;
            _eventBusoptions = eventBusoptions.Value;
            _serviceProvider = serviceProvider;
        }

        public void Connect()
        {
            try
            {
                lock (Lock)
                {
                    if (_channel == null || _channel.IsClosed)
                    {
                        _connection = _connectionChannel.GetConnection();

                        _channel?.Close();

                        _channel?.Dispose();

                        _channel = _connection.CreateModel();

                        _channel.ModelShutdown += async (sender, e) =>
                        {
                            _consumer!.Received -= Received;

                            Connect();

                            Subscribe();

                            await Task.CompletedTask;
                        };

                        _channel.ExchangeDeclare(_rabbitMQOptions.ExchangeName, _rabbitMQOptions.ExchangeType, true);

                    }
                }
            }
            catch (Exception)
            {
                Connect();

                throw;
            }
        }

        public void Subscribe()
        {
            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            _appName = Assembly.GetEntryAssembly()!.GetName().Name;

            string[] dllFiles = Directory.GetFiles(rootDirectory, "*.dll");

            var eventType = typeof(IEvent);

            var handlerType = typeof(IDistributedEventHandler<>);

            List<string> routingKeys = new List<string>();

            foreach (var dll in dllFiles)
            {
                Assembly assembly = Assembly.LoadFrom(dll);

                var handlers = assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract)
                    .Where(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType));

                foreach (var handler in handlers)
                {
                    var eventInterfaces = handler.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType).ToList();

                    foreach (var eventInterface in eventInterfaces)
                    {
                        var eventTypeArgument = eventInterface.GetGenericArguments()[0];

                        if (eventType.IsAssignableFrom(eventTypeArgument))
                        {
                            var routingKey = eventTypeArgument.Name;

                            _routingKeyTypes.Add(eventTypeArgument);

                            routingKeys.Add(routingKey);
                        }
                    }
                }
            }

            foreach (var routingKey in routingKeys)
            {
                var arguments = new Dictionary<string, object> { { "x-message-ttl", _rabbitMQOptions.MessageTTL } };

                if (!string.IsNullOrEmpty(_rabbitMQOptions.QueueMode))
                    arguments.Add("x-queue-mode", _rabbitMQOptions.QueueMode);

                if (!string.IsNullOrEmpty(_rabbitMQOptions.QueueType))
                    arguments.Add("x-queue-type", _rabbitMQOptions.QueueType);

                _channel!.QueueDeclare($"{_appName}.{_rabbitMQOptions.QueueName}", _rabbitMQOptions.Durable, _rabbitMQOptions.Exclusive, _rabbitMQOptions.AutoDelete, arguments);

                _channel.QueueBind($"{_appName}.{_rabbitMQOptions.QueueName}", _rabbitMQOptions.ExchangeName, routingKey);

                _consumer = new AsyncEventingBasicConsumer(_channel);

                _channel!.BasicQos(0, 1, false);

                _channel.BasicConsume(queue: $"{_appName}.{_rabbitMQOptions.QueueName}", autoAck: false, consumer: _consumer);

                _consumer.Received += Received;
            }
        }

        private async Task Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var messageId = e.BasicProperties?.CorrelationId;

                string routingKey = e.RoutingKey;

                Type? eventType = null;

                if (_routingKeyTypes != null && _routingKeyTypes?.Count > 0)
                {
                    eventType = _routingKeyTypes.FirstOrDefault(t => t.Name == e.RoutingKey);
                }

                if (eventType != null)
                {
                    var eventMessageJson = Encoding.UTF8.GetString(e.Body.ToArray());

                    var eventMessage = JsonConvert.DeserializeObject(eventMessageJson, eventType!);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var handler = scope.ServiceProvider.GetService(typeof(IDistributedEventHandler<>).MakeGenericType(eventType));

                        if (handler != null)
                        {

#pragma warning disable CS8600
#pragma warning disable CS8602

                            var retry = 0;

                            var maxRetry = 0;

                            var retryInterval = 0;

                            var props = e.BasicProperties;

                            if (props.Headers != null && props.Headers.TryGetValue("x-retry-time", out var headerValueRetryInterval)) retryInterval = Convert.ToInt32(headerValueRetryInterval);

                            if (props.Headers != null && props.Headers.TryGetValue("x-retry", out var headerValueRetry)) retry = Convert.ToInt32(headerValueRetry);

                            do
                            {
                                try
                                {
                                    maxRetry++;

                                    await (Task)handler.GetType().GetMethod("HandleAsync", new[] { eventMessage.GetType() }).Invoke(handler, new object[] { eventMessage! });

                                    break;
                                }
                                catch
                                {
                                    if (maxRetry > retry) throw;

                                    await Task.Delay(retryInterval * 1000);
                                }
                            }
                            while (true);

#pragma warning restore CS8602
#pragma warning restore CS8600

                            _channel!.BasicAck(e.DeliveryTag, false);
                        }
                    }
                }
                else
                {
                    _channel!.BasicNack(deliveryTag: e.DeliveryTag, multiple: false, requeue: true);
                }
                await Task.CompletedTask;
            }
            catch
            {
                var eventMessageJson = Encoding.UTF8.GetString(e.Body.ToArray());

                _eventBusoptions.FailureCallback?.Invoke(e.RoutingKey, eventMessageJson);

                _channel!.BasicAck(e.DeliveryTag, false);

                throw;
            }
        }

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            return await SendAsync(eventMessage);
        }

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            return Send(eventMessage);
        }

        private Task<bool> SendAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            try
            {
                var routingKey = typeof(TEvent).Name;

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventMessage));

                var props = _channel!.CreateBasicProperties();

                props.DeliveryMode = 2;

                props.Headers = new Dictionary<string, object>
                {
                    { "EventType",routingKey },
                    { "x-retry",_eventBusoptions.RetryCount },
                    { "x-retry-time",_eventBusoptions.RetryInterval }
                };

                _channel.BasicPublish(_rabbitMQOptions.ExchangeName, routingKey, props, body);

                if (_channel.NextPublishSeqNo > 0) _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                Connect();

                throw;
            }
        }

        private bool Send<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            try
            {
                var routingKey = typeof(TEvent).Name;

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventMessage));

                var props = _channel!.CreateBasicProperties();

                props.DeliveryMode = 2;

                props.Headers = new Dictionary<string, object>
                {
                    { "EventType",routingKey },
                    { "x-retry",_eventBusoptions.RetryCount },
                    { "x-retry-time",_eventBusoptions.RetryInterval },
                };

                _channel.BasicPublish(_rabbitMQOptions.ExchangeName, routingKey, props, body);

                if (_channel.NextPublishSeqNo > 0) _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                return true;
            }
            catch (Exception)
            {
                Connect();

                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
