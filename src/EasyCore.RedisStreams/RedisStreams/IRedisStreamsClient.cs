namespace EasyCore.RedisStreams
{
    /// <summary>
    /// Generic Redis Streams publish/subscribe client (not tied to EventBus).
    /// Provides connect, publish, consumer-group subscribe, and acknowledge operations.
    /// </summary>
    public interface IRedisStreamsClient : IAsyncDisposable
    {
        /// <summary>
        /// Connects to Redis using configured endpoints and prepares the consumer group name.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the connect operation.</param>
        /// <returns>A task that completes when the connection is ready.</returns>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a typed JSON payload to the specified Redis stream key.
        /// </summary>
        /// <param name="streamKey">Target stream key.</param>
        /// <param name="typeName">Logical message type name stored in the <c>type</c> field.</param>
        /// <param name="payloadJson">JSON-serialized payload stored in the <c>payload</c> field.</param>
        /// <param name="header">Optional retry metadata; a default header is used when <c>null</c>.</param>
        /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
        /// <returns>A task that completes when the entry has been added to the stream.</returns>
        Task PublishAsync(
            string streamKey,
            string typeName,
            string payloadJson,
            RedisStreamHeader? header = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to the given stream keys via a consumer group and starts a background consume loop.
        /// </summary>
        /// <param name="streamKeys">Stream keys to consume.</param>
        /// <param name="handler">Callback invoked for each delivered message.</param>
        /// <param name="cancellationToken">Token linked to the subscription lifetime.</param>
        /// <returns>A task that completes when the consumer loop has been started.</returns>
        Task SubscribeAsync(
            IEnumerable<string> streamKeys,
            Func<RedisStreamsDeliveredMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acknowledges a previously delivered stream entry for the active consumer group.
        /// </summary>
        /// <param name="streamKey">Stream key that contains the message.</param>
        /// <param name="messageId">Redis stream entry id to acknowledge.</param>
        /// <param name="cancellationToken">Token used to cancel the acknowledge operation.</param>
        /// <returns>A task that completes when the acknowledgment has been sent.</returns>
        Task AcknowledgeAsync(string streamKey, string messageId, CancellationToken cancellationToken = default);
    }
}
