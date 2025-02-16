using EasyCore.EventBus.Event;
using EasyCore.EventBus.RabbitMQ.Exchange.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;
using System.Threading.Channels;

namespace EasyCore.EventBus.RabbitMQ.Exchange.Servers
{
    public class ToipcExchangecs : IToipcExchangecs
    {
        private IModel? _channel;
        private IConnection? _connection;
        private List<Type> _routingKeyTypes = new List<Type>();
        private readonly IConnectionChannel _connectionChannel;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IServiceProvider _serviceProvider;

        public ToipcExchangecs(IConnectionChannel connectionChannel,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            IServiceProvider serviceProvider)
        {
            _connectionChannel = connectionChannel;
            _connection = _connectionChannel!.GetConnection();
            _rabbitMQOptions = rabbitMQOptions.Value;
            _serviceProvider = serviceProvider;
        }

        public void Connect()
        {
            if (_channel == null || _channel.IsClosed)
            {
                _channel = _connection!.CreateModel();

                _channel.ExchangeDeclare(_rabbitMQOptions.ExchangeName, _rabbitMQOptions.ExchangeType, true);

                var arguments = new Dictionary<string, object>
                {
                    { "x-message-ttl", _rabbitMQOptions.MessageTTL }
                };

                if (!string.IsNullOrEmpty(_rabbitMQOptions.QueueMode))
                    arguments.Add("x-queue-mode", _rabbitMQOptions.QueueMode);

                if (!string.IsNullOrEmpty(_rabbitMQOptions.QueueType))
                    arguments.Add("x-queue-type", _rabbitMQOptions.QueueType);

                try
                {
                    _channel.QueueDeclare(_rabbitMQOptions.QueueName, _rabbitMQOptions.Durable, _rabbitMQOptions.Exclusive, _rabbitMQOptions.AutoDelete, arguments);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public void Subscribe()
        {
            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
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
                    var eventInterface = handler.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType);

                    var eventTypeArgument = eventInterface.GetGenericArguments()[0];

                    if (eventType.IsAssignableFrom(eventTypeArgument))
                    {
                        var routingKey = eventTypeArgument.Name;

                        _routingKeyTypes.Add(eventTypeArgument);

                        routingKeys.Add(routingKey);
                    }
                }
            }

            foreach (var routingKey in routingKeys)
            {
                _channel.QueueBind(_rabbitMQOptions.QueueName, _rabbitMQOptions.ExchangeName, routingKey);
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, e) =>
            {
                try
                {
                    var messageId = e.BasicProperties?.CorrelationId;

                    string routingKey = e.RoutingKey;

                    Type? eventType = null;

                    if (_routingKeyTypes != null || _routingKeyTypes?.Count > 0)
                    {
                        eventType = _routingKeyTypes.FirstOrDefault(t => t.Name == e.RoutingKey);
                    }

                    if (eventType != null)
                    {
                        var eventMessageJson = Encoding.UTF8.GetString(e.Body.ToArray());
                        var eventMessage = JsonConvert.DeserializeObject(eventMessageJson, eventType);
                        var handler = _serviceProvider.GetService(typeof(IDistributedEventHandler<>).MakeGenericType(eventType));

                        if (handler != null)
                        {
#pragma warning disable CS8600
#pragma warning disable CS8602
                            await (Task)handler.GetType().GetMethod("HandleAsync").Invoke(handler, new object[] { eventMessage! });
#pragma warning restore CS8602
#pragma warning restore CS8600

                            _channel!.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
                        }
                    }
                    else
                    {
                        _channel!.BasicNack(deliveryTag: e.DeliveryTag, multiple: false, requeue: true);
                    }
                    await Task.CompletedTask;
                }
                catch (Exception)
                {
                    _channel!.BasicNack(deliveryTag: e.DeliveryTag, multiple: false, requeue: true);
                    throw;
                }
            };

            _channel.BasicConsume(queue: _rabbitMQOptions.QueueName, autoAck: false, consumer: consumer);
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

                if (_channel == null || _channel.IsClosed)
                {
                    Connect();
                }

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventMessage));

                var props = _channel!.CreateBasicProperties();

                props.DeliveryMode = 2;

                props.Headers = new Dictionary<string, object>
                {
                    { "EventType",typeof(TEvent).Name }
                };

                _channel.BasicPublish(_rabbitMQOptions.ExchangeName, routingKey, props, body);

                if (_channel.NextPublishSeqNo > 0) _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool Send<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            try
            {
                var routingKey = typeof(TEvent).Name;

                if (_channel == null || _channel.IsClosed)
                {
                    Connect();
                }

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventMessage));

                var props = _channel!.CreateBasicProperties();

                props.DeliveryMode = 2;

                props.Headers = new Dictionary<string, object>
                {
                    { "EventType",typeof(TEvent).Name }
                };

                _channel.BasicPublish(_rabbitMQOptions.ExchangeName, routingKey, props, body);

                if (_channel.NextPublishSeqNo > 0) _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
