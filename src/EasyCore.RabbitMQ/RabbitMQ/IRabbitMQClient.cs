namespace EasyCore.RabbitMQ
{
    /// <summary>
    /// Generic RabbitMQ publish/subscribe client (not tied to EventBus).
    /// Provides connection, publish, subscribe, and acknowledge operations over AMQP.
    /// </summary>
    public interface IRabbitMQClient : IAsyncDisposable
    {
        /// <summary>
        /// Connects to the broker and declares the configured exchange.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the connect operation.</param>
        /// <returns>A task that completes when the connection and exchange are ready.</returns>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a message with the given routing key to the configured exchange.
        /// </summary>
        /// <param name="routingKey">Routing key used to route the message.</param>
        /// <param name="body">Raw message body bytes.</param>
        /// <param name="headers">Optional AMQP headers to attach to the message.</param>
        /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
        /// <returns>A task that completes when the message has been published and confirmed.</returns>
        Task PublishAsync(
            string routingKey,
            ReadOnlyMemory<byte> body,
            IDictionary<string, object>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to routing keys. Declares one queue, binds all keys, and starts consuming.
        /// </summary>
        /// <param name="routingKeys">Routing keys to bind to the subscription queue.</param>
        /// <param name="handler">Callback invoked for each delivered message.</param>
        /// <param name="cancellationToken">Token linked to the subscription lifetime.</param>
        /// <returns>A task that completes when the consumer has been started.</returns>
        Task SubscribeAsync(
            IEnumerable<string> routingKeys,
            Func<RabbitMQDeliveredMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acknowledges a delivered message so it is removed from the queue.
        /// </summary>
        /// <param name="deliveryTag">Delivery tag of the message to acknowledge.</param>
        void Ack(ulong deliveryTag);

        /// <summary>
        /// Negatively acknowledges a delivered message.
        /// </summary>
        /// <param name="deliveryTag">Delivery tag of the message to reject.</param>
        /// <param name="requeue">
        /// When <c>true</c>, the message is requeued; otherwise it is discarded or dead-lettered.
        /// </param>
        void Nack(ulong deliveryTag, bool requeue);
    }
}
