using System.Text.Json;

namespace EasyCore.RedisStreams
{
    /// <summary>
    /// Unified Redis Streams entry field names and header serialization helpers.
    /// </summary>
    public static class RedisStreamMessageFormat
    {
        /// <summary>
        /// Field name for the serialized <see cref="RedisStreamHeader"/> JSON.
        /// </summary>
        public const string HeaderField = "RedisHeader";

        /// <summary>
        /// Field name for the JSON message payload.
        /// </summary>
        public const string PayloadField = "payload";

        /// <summary>
        /// Field name for the logical message type.
        /// </summary>
        public const string TypeField = "type";

        /// <summary>
        /// Serializes a <see cref="RedisStreamHeader"/> to JSON for storage in the stream entry.
        /// </summary>
        /// <param name="header">Header instance to serialize.</param>
        /// <returns>JSON string representation of <paramref name="header"/>.</returns>
        public static string SerializeHeader(RedisStreamHeader header) => JsonSerializer.Serialize(header);

        /// <summary>
        /// Deserializes a <see cref="RedisStreamHeader"/> from JSON stored in a stream entry.
        /// </summary>
        /// <param name="json">JSON string previously produced by <see cref="SerializeHeader"/>.</param>
        /// <returns>Deserialized header, or <c>null</c> if deserialization fails or input is invalid.</returns>
        public static RedisStreamHeader? DeserializeHeader(string json) => JsonSerializer.Deserialize<RedisStreamHeader>(json);
    }

    /// <summary>
    /// Retry metadata stored with each Redis Streams entry.
    /// </summary>
    public class RedisStreamHeader
    {
        /// <summary>
        /// Number of times processing of this message has been retried.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Retry interval in milliseconds (or the unit agreed by the EventBus integration).
        /// </summary>
        public int RetryInterval { get; set; }
    }
}
