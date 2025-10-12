using EasyCore.EventBus.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Pulsar.Client.Api;
using System.Text;

namespace EasyCore.EventBus.Pulsar.Exchange
{
    public class PulsarExchangecs : IPulsarExchangecs
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EventBusOptions _eventBusoptions;
        private readonly IConnectionChannel _connectionChannel;
        private PulsarClient? _pulsarClient;
        private List<Type> _topics;
        private IConsumer<byte[]>? _consumer;

        public PulsarExchangecs(
            IOptions<EventBusOptions> eventBusoptions,
            IServiceProvider serviceProvider,
            IConnectionChannel connectionChannel)
        {
            _eventBusoptions = eventBusoptions.Value;
            _serviceProvider = serviceProvider;
            _connectionChannel = connectionChannel;
            _topics = _connectionChannel.CreateTopic();
        }

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent => await SendAsync(eventMessage);

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent => Send(eventMessage);

        private async Task<bool> SendAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            await Connect();

            var producer = await _connectionChannel.PulsarProducerAsync(eventMessage.GetType().Name, _pulsarClient);

            try
            {
                var header = new Dictionary<string, string?>
                {
                    ["EventType"] = eventMessage.GetType().Name,
                    ["x-retry"] = _eventBusoptions.RetryCount.ToString(),
                    ["x-retry-time"] = _eventBusoptions.RetryInterval.ToString()
                };

                var pulsarMessage = producer.NewMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventMessage)), eventMessage.GetType().Name, header);

                var messageId = await producer.SendAsync(pulsarMessage);

                return messageId != null;
            }
            catch
            {
                throw;
            }
            finally
            {
                await producer.DisposeAsync();
            }
        }

        private bool Send<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            Connect().Wait();

            var producer = _connectionChannel.PulsarProducerAsync(eventMessage.GetType().Name, _pulsarClient).Result;

            try
            {
                var header = new Dictionary<string, string?>
                {
                    ["EventType"] = eventMessage.GetType().Name,
                    ["x-retry"] = _eventBusoptions.RetryCount.ToString(),
                    ["x-retry-time"] = _eventBusoptions.RetryInterval.ToString()
                };

                var pulsarMessage = producer.NewMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventMessage)), eventMessage.GetType().Name, header);

                var messageId = producer.SendAsync(pulsarMessage).Result;

                return messageId != null;
            }
            catch
            {
                throw;
            }
            finally
            {
                producer.DisposeAsync();
            }
        }

        public async Task Connect() => _pulsarClient = await _connectionChannel.PulsarClientAsync(_pulsarClient);

        public async Task Subscribe()
        {
            await Connect();

            _consumer = await _connectionChannel.PulsarConsumerAsync(_pulsarClient);

            if (_consumer is not null) ExecuteAsync(_consumer);
        }

        public void ExecuteAsync(IConsumer<byte[]> consumer)
        {
            if (consumer is null) return;

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var consumerMessage = await consumer.ReceiveAsync();

                    try
                    {
                        if (consumerMessage == null) continue;

                        var headers = new Dictionary<string, string?>(consumerMessage.Properties.Count);

                        foreach (var header in consumerMessage.Properties) headers.Add(header.Key, header.Value);

                        if (headers == null || headers.Count <= 0) continue;

                        Type? eventType = null;

                        if (_topics != null && _topics?.Count > 0) eventType = _topics.FirstOrDefault(t => t.Name == headers["EventType"]);

                        if (eventType == null) continue;

                        var eventMessage = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(consumerMessage.Data), eventType);

                        using (var scope = _serviceProvider.CreateScope())
                        {
#pragma warning disable CS8600
#pragma warning disable CS8602

                            var handler = scope.ServiceProvider.GetService(typeof(IDistributedEventHandler<>).MakeGenericType(eventType));

                            if (handler is null) continue;

                            var retry = 0;

                            var maxRetry = 0;

                            var retryInterval = 0;

                            if (int.TryParse(headers["x-retry-time"], out var headerValueRetryInterval)) retryInterval = Convert.ToInt32(headerValueRetryInterval);

                            if (int.TryParse(headers["x-retry"], out var headerValueRetry)) retry = Convert.ToInt32(headerValueRetry);

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

                            await consumer.AcknowledgeAsync(consumerMessage.MessageId);

#pragma warning restore CS8602
#pragma warning restore CS8600
                        }
                    }
                    catch
                    {
                        var headers = new Dictionary<string, string?>(consumerMessage.Properties.Count);

                        foreach (var header in consumerMessage.Properties) headers.Add(header.Key, header.Value);

                        var eventTypeHeader = headers["EventType"]!;

                        var eventMessage = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(consumerMessage.Data)).ToString();

                        await consumer.AcknowledgeAsync(consumerMessage.MessageId);

                        _eventBusoptions.FailureCallback?.Invoke(eventTypeHeader, eventMessage);
                    }
                }
            });
        }

        public void Dispose()
        {
            _pulsarClient?.CloseAsync().Wait();

            _consumer?.DisposeAsync();
        }
    }
}
