using EasyCore.EventBus.Kafka.Kafka;

namespace EasyCore.EventBus.Kafka
{
    public static class EventBusKafkaExtensions
    {
        public static EventBusOptions Kafka(this EventBusOptions options, string bootstrapServers)
        {
            if (string.IsNullOrEmpty(bootstrapServers)) throw new ArgumentException(nameof(bootstrapServers));

            var configure = new Action<KafkaOptions>(options =>
            {
                options.BootstrapServers = bootstrapServers;
            });

            options.RegisterExtension(new KafkaOptionsExtension(configure));

            return options;
        }

        public static EventBusOptions Kafka(this EventBusOptions options, Action<KafkaOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var KafkaOptions = new KafkaOptions();

            configure.Invoke(KafkaOptions);

            options.RegisterExtension(new KafkaOptionsExtension(configure));

            return options;
        }
    }
}
