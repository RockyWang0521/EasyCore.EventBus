namespace EasyCore.RedisStreams
{
    /// <summary>
    /// A message delivered from Redis Streams to a subscriber handler.
    /// </summary>
    public sealed class RedisStreamsDeliveredMessage
    {
        /// <summary>
        /// Stream key from which the entry was read.
        /// </summary>
        public required string StreamKey { get; init; }

        /// <summary>
        /// Redis stream entry id used for <see cref="IRedisStreamsClient.AcknowledgeAsync"/>.
        /// </summary>
        public required string MessageId { get; init; }

        /// <summary>
        /// Logical message type name from the <c>type</c> field.
        /// </summary>
        public required string TypeName { get; init; }

        /// <summary>
        /// JSON payload from the <c>payload</c> field.
        /// </summary>
        public required string PayloadJson { get; init; }

        /// <summary>
        /// Retry metadata deserialized from the header field.
        /// </summary>
        public RedisStreamHeader Header { get; init; } = new();
    }
}
