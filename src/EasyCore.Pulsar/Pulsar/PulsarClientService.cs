using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pulsar.Client.Api;
using Pulsar.Client.Common;
using System.Reflection;

namespace EasyCore.Pulsar
{
    /// <summary>
    /// Default <see cref="IPulsarClient"/> implementation.
    /// Manages the Pulsar client, a shared subscription consumer, and a background consume loop.
    /// </summary>
    public sealed class PulsarClientService : IPulsarClient
    {
        /// <summary>
        /// Resolved Pulsar connection options.
        /// </summary>
        private readonly PulsarOptions _options;

        /// <summary>
        /// Logger used for diagnostics and error reporting.
        /// </summary>
        private readonly ILogger<PulsarClientService> _logger;

        /// <summary>
        /// Underlying Pulsar client, or <c>null</c> until <see cref="ConnectAsync"/> succeeds.
        /// </summary>
        private PulsarClient? _client;

        /// <summary>
        /// Active Pulsar consumer for the subscription, or <c>null</c> when not subscribed.
        /// </summary>
        private IConsumer<byte[]>? _consumer;

        /// <summary>
        /// Linked cancellation source for the active consume loop.
        /// </summary>
        private CancellationTokenSource? _linkedCts;

        /// <summary>
        /// Background task that receives and dispatches messages.
        /// </summary>
        private Task? _consumeLoop;

        /// <summary>
        /// Indicates whether this instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PulsarClientService"/> class.
        /// </summary>
        /// <param name="options">Pulsar options monitored via <see cref="IOptions{TOptions}"/>.</param>
        /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
        public PulsarClientService(IOptions<PulsarOptions> options, ILogger<PulsarClientService>? logger = null)
        {
            _options = options.Value;
            _logger = logger ?? NullLogger<PulsarClientService>.Instance;
        }

        /// <inheritdoc />
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            if (_client != null)
                return;

            var builder = new PulsarClientBuilder().ServiceUrl(_options.ServiceUrl);
            _client = await builder.BuildAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task PublishAsync(
            string topic,
            ReadOnlyMemory<byte> body,
            IDictionary<string, string>? properties = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(topic);
            await ConnectAsync(cancellationToken).ConfigureAwait(false);

            var fullTopic = topic.StartsWith("persistent://", StringComparison.OrdinalIgnoreCase)
                ? topic
                : $"{_options.TopicPrefix.TrimEnd('/')}/{topic}";

            var producer = await _client!.NewProducer().Topic(fullTopic).CreateAsync().ConfigureAwait(false);
            try
            {
                Dictionary<string, string?>? props = null;
                if (properties != null)
                {
                    props = new Dictionary<string, string?>();
                    foreach (var kv in properties)
                        props[kv.Key] = kv.Value;
                }

                var message = producer.NewMessage(body.ToArray(), topic, props);
                var messageId = await producer.SendAsync(message).ConfigureAwait(false);
                if (messageId == null)
                    throw new InvalidOperationException($"Pulsar publish to topic '{fullTopic}' failed.");
            }
            finally
            {
                await producer.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task SubscribeAsync(
            IEnumerable<string> topics,
            Func<PulsarDeliveredMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(handler);
            await ConnectAsync(cancellationToken).ConfigureAwait(false);

            var topicList = topics
                .Select(t => t.StartsWith("persistent://", StringComparison.OrdinalIgnoreCase)
                    ? t
                    : $"{_options.TopicPrefix.TrimEnd('/')}/{t}")
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (topicList.Count == 0)
            {
                _logger.LogWarning("Pulsar SubscribeAsync called with no topics.");
                return;
            }

            var appName = _options.AppName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "EasyCore";

            _consumer = await _client!.NewConsumer()
                .Topics(topicList)
                .SubscriptionName($"{appName}.PulsarTopic")
                .SubscriptionType(SubscriptionType.Shared)
                .ConsumerName($"{appName}.Consumer.{Guid.NewGuid():N}")
                .SubscribeAsync()
                .ConfigureAwait(false);

            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _linkedCts.Token;

            _consumeLoop = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var consumerMessage = await _consumer.ReceiveAsync().ConfigureAwait(false);
                        if (consumerMessage == null)
                            continue;

                        var props = new Dictionary<string, string>(StringComparer.Ordinal);
                        foreach (var header in consumerMessage.Properties)
                            props[header.Key] = header.Value ?? string.Empty;

                        var topic = props.TryGetValue("EventType", out var eventType)
                            ? eventType
                            : string.Empty;

                        var delivered = new PulsarDeliveredMessage
                        {
                            Topic = topic,
                            Body = consumerMessage.Data,
                            Properties = props,
                            MessageId = consumerMessage.MessageId
                        };

                        await handler(delivered, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Pulsar consume loop error.");
                    }
                }
            }, ct);

            _logger.LogInformation("Pulsar subscribed to {Count} topics.", topicList.Count);
        }

        /// <inheritdoc />
        public async Task AcknowledgeAsync(MessageId messageId, CancellationToken cancellationToken = default)
        {
            if (_consumer == null)
                return;

            cancellationToken.ThrowIfCancellationRequested();
            await _consumer.AcknowledgeAsync(messageId).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancels the consume loop, disposes the consumer and client, and releases resources.
        /// </summary>
        /// <returns>A value task that completes when disposal finishes.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            _linkedCts?.Cancel();

            if (_consumeLoop != null)
            {
                try
                {
                    await _consumeLoop.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (_consumer != null)
                await _consumer.DisposeAsync().ConfigureAwait(false);

            if (_client != null)
                await _client.CloseAsync().ConfigureAwait(false);

            _linkedCts?.Dispose();
        }
    }
}
