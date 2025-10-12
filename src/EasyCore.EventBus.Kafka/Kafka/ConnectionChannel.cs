using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace EasyCore.EventBus.Kafka.Kafka
{
    public class ConnectionChannel : IConnectionChannel
    {
        private readonly KafkaOptions _options;
        private string? _appName;

        public ConnectionChannel(IOptions<KafkaOptions> options)
        {
            _options = options.Value;
            _appName = Assembly.GetEntryAssembly()!.GetName().Name;
        }

        public bool CloseProducer(IProducer<string, string>? producer)
        {
            if (producer != null)
            {
                producer.Dispose();

                producer = null;

                return true;
            }

            return true;
        }

        public IConsumer<string, string> CreateConsumer(IConsumer<string, string>? consumer)
        {
            if (consumer != null) return consumer;

            var config = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.RoundRobin,
                GroupId = $"{_appName}.{_options.GroupId}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = true,
                EnableAutoCommit = false,
                LogConnectionClose = false,
            };

            return new ConsumerBuilder<string, string>(config).Build();
        }

        public IProducer<string, string> CreateProducer(IProducer<string, string>? producer)
        {
            if (producer != null) return producer;

            var config = new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                QueueBufferingMaxMessages = _options.QueueBufferingMaxMessages,
                MessageTimeoutMs = _options.MessageTimeoutMs,
                RequestTimeoutMs = _options.RequestTimeoutMs,
            };

            return new ProducerBuilder<string, string>(config).Build();
        }

        public IProducer<string, string> CreateProducer()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                QueueBufferingMaxMessages = _options.QueueBufferingMaxMessages,
                MessageTimeoutMs = _options.MessageTimeoutMs,
                RequestTimeoutMs = _options.RequestTimeoutMs,
            };

            return new ProducerBuilder<string, string>(config).Build();
        }
    }
}
