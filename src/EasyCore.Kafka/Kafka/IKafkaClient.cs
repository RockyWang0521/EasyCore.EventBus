namespace EasyCore.Kafka
{
    /// <summary>
    /// Generic Kafka publish/subscribe client (not tied to EventBus).
    /// </summary>
    public interface IKafkaClient : IAsyncDisposable
    {
        /// <summary>
        /// Creates the Kafka consumer when it has not already been initialized.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the connect operation.</param>
        /// <returns>A task that completes when the consumer is ready.</returns>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a message to the specified Kafka topic.
        /// </summary>
        /// <param name="topic">Target topic name.</param>
        /// <param name="body">Raw message payload.</param>
        /// <param name="key">Optional message key used for partitioning.</param>
        /// <param name="headers">Optional Kafka headers as byte arrays.</param>
        /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
        /// <returns>A task that completes when the message has been produced.</returns>
        Task PublishAsync(
            string topic,
            ReadOnlyMemory<byte> body,
            string? key = null,
            IDictionary<string, byte[]>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to the specified topics and starts a background consume loop.
        /// </summary>
        /// <param name="topics">Topics to subscribe to.</param>
        /// <param name="handler">Callback invoked for each delivered message.</param>
        /// <param name="cancellationToken">Token linked to the subscription lifetime.</param>
        /// <returns>A task that completes when the consume loop has been started.</returns>
        Task SubscribeAsync(
            IEnumerable<string> topics,
            Func<KafkaDeliveredMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the last consumed message associated with <paramref name="nativeResult"/>.
        /// </summary>
        /// <param name="nativeResult">Native Kafka consume result previously delivered to the handler.</param>
        void Commit(object nativeResult);
    }
}
