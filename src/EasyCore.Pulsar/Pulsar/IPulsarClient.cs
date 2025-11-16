namespace EasyCore.Pulsar
{
    /// <summary>
    /// Generic Pulsar publish/subscribe client (not tied to EventBus).
    /// Provides connect, publish, subscribe, and acknowledge operations.
    /// </summary>
    public interface IPulsarClient : IAsyncDisposable
    {
        /// <summary>
        /// Connects to the Pulsar service URL and builds the underlying client.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the connect operation.</param>
        /// <returns>A task that completes when the client is ready.</returns>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a message to the specified Pulsar topic.
        /// </summary>
        /// <param name="topic">
        /// Topic name or fully-qualified persistent URL.
        /// Relative names are prefixed with <see cref="PulsarOptions.TopicPrefix"/>.
        /// </param>
        /// <param name="body">Raw message body bytes.</param>
        /// <param name="properties">Optional string properties attached to the message.</param>
        /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
        /// <returns>A task that completes when the message has been sent.</returns>
        Task PublishAsync(
            string topic,
            ReadOnlyMemory<byte> body,
            IDictionary<string, string>? properties = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to the given topics and starts a background consume loop that invokes <paramref name="handler"/>.
        /// </summary>
        /// <param name="topics">Topics to subscribe to (relative or fully-qualified).</param>
        /// <param name="handler">Callback invoked for each delivered message.</param>
        /// <param name="cancellationToken">Token linked to the subscription lifetime.</param>
        /// <returns>A task that completes when the consumer loop has been started.</returns>
        Task SubscribeAsync(
            IEnumerable<string> topics,
            Func<PulsarDeliveredMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acknowledges a previously delivered message by its Pulsar message id.
        /// </summary>
        /// <param name="messageId">Pulsar message identifier to acknowledge.</param>
        /// <param name="cancellationToken">Token used to cancel the acknowledge operation.</param>
        /// <returns>A task that completes when the acknowledgment has been sent.</returns>
        Task AcknowledgeAsync(global::Pulsar.Client.Common.MessageId messageId, CancellationToken cancellationToken = default);
    }
}
