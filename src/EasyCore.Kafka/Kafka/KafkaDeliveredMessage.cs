namespace EasyCore.Kafka
{
    /// <summary>
    /// A message delivered from Kafka.
    /// </summary>
    public sealed class KafkaDeliveredMessage
    {
        public required string Topic { get; init; }

        public required string? Key { get; init; }

        public required ReadOnlyMemory<byte> Body { get; init; }

        public IReadOnlyDictionary<string, byte[]> Headers { get; init; } = new Dictionary<string, byte[]>();

        public object? NativeResult { get; init; }
    }
}
