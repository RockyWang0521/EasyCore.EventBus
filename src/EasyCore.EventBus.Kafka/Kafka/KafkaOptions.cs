namespace EasyCore.EventBus.Kafka.Kafka
{
    public class KafkaOptions
    {
        /// <summary>
        /// Kafka cluster addresses, multiple addresses should be separated by commas (e.g., 192.168.10.11:9092,192.168.10.12:9092)
        /// </summary>
        public string BootstrapServers { get; set; } = "localhost:9092";

        /// <summary>
        /// Kafka
        /// </summary>
        public string TopicName { get; set; } = "EasyCore.Topic";

        /// <summary>
        /// Kafka GroupId
        /// </summary>
        public string GroupId { get; set; } = "EasyCore.GroupId";

        /// <summary>
        /// Message send timeout (throws an exception when exceeded)
        /// </summary>
        public int MessageTimeoutMs { get; set; } = 10;

        /// <summary>
        /// Request timeout (default: 10000ms)
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Queue buffer size (default: 3000)
        /// </summary>
        public int QueueBufferingMaxMessages { get; set; } = 30000;
    }
}
