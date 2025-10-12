using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EasyCore.EventBus.Event;
using EasyCore.EventBus.Kafka.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace EasyCore.EventBus.Kafka.Exchange
{
    public class KafkaExchangecs : IKafkaExchangecs
    {
        private readonly IConnectionChannel _connectionChannel;
        private IConsumer<string, string>? _consumer;
        private readonly KafkaOptions _kafkaOptions;
        private readonly EventBusOptions _eventBusOptions;
        private readonly IServiceProvider _serviceProvider;
        private List<Type> _keys = new List<Type>();

        public KafkaExchangecs(
            IConnectionChannel connectionChannel,
            IOptions<KafkaOptions> kafkaOptions,
            IOptions<EventBusOptions> eventBusOptions,
            IServiceProvider serviceProvider)
        {
            _connectionChannel = connectionChannel;
            _kafkaOptions = kafkaOptions.Value;
            _eventBusOptions = eventBusOptions.Value;
            _serviceProvider = serviceProvider;
            CreateTopic();
        }

        public bool Publish<TEvent>(TEvent eventMessage) => Send(eventMessage);

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) => await SendAsync(eventMessage);

        public void Connect() => _consumer = _connectionChannel.CreateConsumer(_consumer);

        public bool Send<TEvent>(TEvent eventMessage)
        {
            var producer = _connectionChannel.CreateProducer();

            try
            {
                var headers = new Headers();

                headers.Add("EventType", Encoding.UTF8.GetBytes(typeof(TEvent).Name!));

                headers.Add("x-retry", Encoding.UTF8.GetBytes(_eventBusOptions.RetryCount.ToString()));

                headers.Add("x-retry-time", Encoding.UTF8.GetBytes(_eventBusOptions.RetryInterval.ToString()));

                var result = producer.ProduceAsync($"{_kafkaOptions.TopicName}.{eventMessage!.GetType().Name}", new Message<string, string>
                {
                    Headers = headers,
                    Value = JsonConvert.SerializeObject(eventMessage),
                    Key = typeof(TEvent).Name!
                }).Result;

                if (result.Status is PersistenceStatus.Persisted or PersistenceStatus.PossiblyPersisted) return true;

                return false;
            }
            catch (Exception)
            {
                _connectionChannel.CloseProducer(producer);

                return false;
            }
            finally
            {
                producer.Dispose();
            }
        }

        public async Task<bool> SendAsync<TEvent>(TEvent eventMessage)
        {
            var producer = _connectionChannel.CreateProducer();

            try
            {
                var headers = new Headers();

                headers.Add("EventType", Encoding.UTF8.GetBytes(typeof(TEvent).Name!));

                headers.Add("x-retry", Encoding.UTF8.GetBytes(_eventBusOptions.RetryCount.ToString()));

                headers.Add("x-retry-time", Encoding.UTF8.GetBytes(_eventBusOptions.RetryInterval.ToString()));

                var result = await producer.ProduceAsync($"{_kafkaOptions.TopicName}.{eventMessage!.GetType().Name}", new Message<string, string>
                {
                    Headers = headers,
                    Value = JsonConvert.SerializeObject(eventMessage),
                    Key = typeof(TEvent).Name!
                });

                if (result.Status is PersistenceStatus.Persisted or PersistenceStatus.PossiblyPersisted) return true;

                return false;
            }
            catch (Exception)
            {
                _connectionChannel.CloseProducer(producer);

                return false;
            }
            finally
            {
                producer.Dispose();
            }
        }

        private void CreateTopic()
        {
            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string[] dllFiles = Directory.GetFiles(rootDirectory, "*.dll");

            _keys.Clear();

            var eventType = typeof(IEvent);

            var handlerType = typeof(IDistributedEventHandler<>);

            List<string> topics = new List<string>();

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

                        if (eventType.IsAssignableFrom(eventTypeArgument)) _keys.Add(eventTypeArgument);
                    }
                }
            }

            if (_keys.Count <= 0) return;

            var config = new AdminClientConfig { BootstrapServers = _kafkaOptions.BootstrapServers };

            using var adminClient = new AdminClientBuilder(config).Build();

            var topicSpecs = new List<TopicSpecification>();

            foreach (var key in _keys)
            {
                var topicSpec = new TopicSpecification { Name = $"{_kafkaOptions.TopicName}.{key.Name}" };

                topicSpecs.Add(topicSpec);
            }

            try
            {
                adminClient.CreateTopicsAsync(topicSpecs).GetAwaiter().GetResult();
            }
            catch (CreateTopicsException ex) when (ex.Message.Contains("already exists"))
            {
            }
        }

        public void Subscribe()
        {
            if (_consumer == null) return;

            if (_keys.Count <= 0) return;

            var topics = new List<string>();

            foreach (var key in _keys) { topics.Add($"{_kafkaOptions.TopicName}.{key.Name}"); }

            _consumer!.Subscribe(topics);

            var _cts = new CancellationTokenSource();

            ExecuteAsync(_cts.Token);
        }

        public void ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_consumer is null) return;

            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumerResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                    try
                    {
                        if (consumerResult == null) continue;

                        if (consumerResult.IsPartitionEOF || consumerResult.Message.Value == null) continue;

                        var headers = consumerResult.Message.Headers;

                        Type? eventType = null;

                        if (_keys != null && _keys?.Count > 0)
                        {
                            var eventTypeHeader = Encoding.UTF8.GetString(headers.GetLastBytes("EventType"));

                            eventType = _keys.FirstOrDefault(t => t.Name == eventTypeHeader);
                        }

                        if (eventType == null) continue;

                        var eventMessage = JsonConvert.DeserializeObject(consumerResult.Message.Value, eventType);

                        using (var scope = _serviceProvider.CreateScope())
                        {
#pragma warning disable CS8600
#pragma warning disable CS8602

                            var handler = scope.ServiceProvider.GetService(typeof(IDistributedEventHandler<>).MakeGenericType(eventType));

                            if (handler is null) continue;

                            var retry = 0;

                            var maxRetry = 0;

                            var retryInterval = 0;

                            if (int.TryParse(Encoding.UTF8.GetString(headers.GetLastBytes("x-retry-time")), out var headerValueRetryInterval)) retryInterval = Convert.ToInt32(headerValueRetryInterval);

                            if (int.TryParse(Encoding.UTF8.GetString(headers.GetLastBytes("x-retry")), out var headerValueRetry)) retry = Convert.ToInt32(headerValueRetry);

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
                            } while (true);

                            _consumer.Commit(consumerResult);

#pragma warning restore CS8602
#pragma warning restore CS8600
                        }
                    }
                    catch
                    {
                        var headers = consumerResult.Message.Headers;

                        var eventTypeHeader = Encoding.UTF8.GetString(headers.GetLastBytes("EventType"));

                        var eventMessage = JsonConvert.DeserializeObject(consumerResult.Message.Value)?.ToString();

                        _consumer.Commit(consumerResult);

                        _eventBusOptions.FailureCallback?.Invoke(eventTypeHeader, eventMessage);
                    }
                }
            });
        }

        public void Dispose() => _consumer?.Dispose();
    }
}
