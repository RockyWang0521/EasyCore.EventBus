namespace EasyCore.RabbitMQ
{
    /// <summary>
    /// Represents a message delivered from RabbitMQ to a subscriber.
    /// </summary>
    public sealed class RabbitMQDeliveredMessage
    {
        /// <summary>
        /// Gets the routing key associated with the delivered message.
        /// </summary>
        public required string RoutingKey { get; init; }

        /// <summary>
        /// Gets the raw message body.
        /// </summary>
        public required ReadOnlyMemory<byte> Body { get; init; }

        /// <summary>
        /// Gets optional AMQP headers attached to the message.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Headers { get; init; }

        /// <summary>
        /// Gets the broker delivery tag used for ack/nack.
        /// </summary>
        public ulong DeliveryTag { get; init; }

        /// <summary>
        /// Gets the optional correlation identifier from message properties.
        /// </summary>
        public string? CorrelationId { get; init; }
    }
}
