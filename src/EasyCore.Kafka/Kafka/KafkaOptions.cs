namespace EasyCore.Kafka
{
    /// <summary>
    /// Kafka connection and producer/consumer options.
    /// </summary>
    public class KafkaOptions
    {
        /// <summary>
        /// Bootstrap servers, comma-separated.
        /// </summary>
        public string BootstrapServers { get; set; } = "localhost:9092";

        /// <summary>
        /// Topic name prefix.
        /// </summary>
        public string TopicName { get; set; } = "EasyCore.Topic";

        /// <summary>
        /// Consumer group id suffix.
        /// </summary>
        public string GroupId { get; set; } = "EasyCore.GroupId";

        /// <summary>
        /// Message send timeout in milliseconds.
        /// </summary>
        public int MessageTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Request timeout in milliseconds.
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Producer queue buffer size.
        /// </summary>
        public int QueueBufferingMaxMessages { get; set; } = 30000;

        /// <summary>
        /// Application name used for group id. When null, entry assembly name is used.
        /// </summary>
        public string? AppName { get; set; }
    }
}
