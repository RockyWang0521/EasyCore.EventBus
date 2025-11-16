using Pulsar.Client.Common;

namespace EasyCore.Pulsar
{
    /// <summary>
    /// A message delivered from Pulsar to a subscriber handler.
    /// </summary>
    public sealed class PulsarDeliveredMessage
    {
        /// <summary>
        /// Logical topic / event type associated with the message
        /// (typically taken from the <c>EventType</c> property when present).
        /// </summary>
        public required string Topic { get; init; }

        /// <summary>
        /// Raw message body bytes.
        /// </summary>
        public required ReadOnlyMemory<byte> Body { get; init; }

        /// <summary>
        /// Pulsar message properties as string key/value pairs.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// Pulsar message identifier used for <see cref="IPulsarClient.AcknowledgeAsync"/>.
        /// </summary>
        public required MessageId MessageId { get; init; }
    }
}
