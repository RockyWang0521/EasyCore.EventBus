using EasyCore.EventBus.Event;
using Microsoft.Extensions.Options;
using Pulsar.Client.Api;
using Pulsar.Client.Common;
using System.Reflection;

namespace EasyCore.EventBus.Pulsar
{
    public class ConnectionChannel : IConnectionChannel
    {
        private readonly PulsarOptions _pulsarOptions;
        private List<string> _topicNames = new List<string>();
        private string? _appName;
        const string _defaultTopic = "persistent://public/default/";

        public ConnectionChannel(IOptions<PulsarOptions> pulsarOptions)
        {
            _pulsarOptions = pulsarOptions.Value;
            _appName = Assembly.GetEntryAssembly()!.GetName().Name;
        }

        public async Task<PulsarClient> PulsarClientAsync(PulsarClient? pulsarClient)
        {
            if (pulsarClient is null) pulsarClient = await new PulsarClientBuilder().ServiceUrl(_pulsarOptions.ServiceUrl).BuildAsync();

            return pulsarClient;
        }

        public async Task<IProducer<byte[]>> PulsarProducerAsync(string topic, PulsarClient? pulsarClient)
        {
            pulsarClient = await PulsarClientAsync(pulsarClient);

            return await pulsarClient.NewProducer().Topic($"{_defaultTopic}{topic}").CreateAsync();
        }

        public async Task<IConsumer<byte[]>?> PulsarConsumerAsync(PulsarClient? pulsarClient)
        {
            if (_topicNames.Count <= 0) return null;

            pulsarClient = await PulsarClientAsync(pulsarClient);

            return await pulsarClient.NewConsumer().Topics(_topicNames).SubscriptionName($"{_appName}.PulsarTpoic")
                .SubscriptionType(SubscriptionType.Shared)
                .ConsumerName($"{_appName}.Consumer.{Guid.NewGuid()}")
                .SubscribeAsync();
        }

        public List<Type> CreateTopic()
        {
            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string[] dllFiles = Directory.GetFiles(rootDirectory, "*.dll");

            var eventType = typeof(IEvent);

            var handlerType = typeof(IDistributedEventHandler<>);

            List<Type> topics = new List<Type>();

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
                            topics.Add(eventTypeArgument);

                            _topicNames.Add($"{_defaultTopic}{eventTypeArgument.Name}");
                        }
                    }
                }
            }

            return topics;
        }
    }
}
