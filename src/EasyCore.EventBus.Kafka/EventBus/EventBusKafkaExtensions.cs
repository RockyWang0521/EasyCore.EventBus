namespace EasyCore.EventBus.Kafka
{
    public static class EventBusKafkaExtensions
    {
        public static EventBusOptions Kafka(this EventBusOptions options, string bootstrapServers)
        {
            if (string.IsNullOrEmpty(bootstrapServers))
                throw new ArgumentException("Bootstrap servers are required.", nameof(bootstrapServers));

            return options.Kafka(opt => opt.BootstrapServers = bootstrapServers);
        }

        public static EventBusOptions Kafka(this EventBusOptions options, Action<KafkaOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            options.RegisterExtension(new KafkaOptionsExtension(configure));
            return options;
        }
    }
}
