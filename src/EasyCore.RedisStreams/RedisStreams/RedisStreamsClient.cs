using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Reflection;

namespace EasyCore.RedisStreams
{
    /// <summary>
    /// Default <see cref="IRedisStreamsClient"/> implementation.
    /// Manages the Redis multiplexer, consumer group, and a background XREADGROUP loop.
    /// </summary>
    public sealed class RedisStreamsClient : IRedisStreamsClient
    {
        /// <summary>
        /// Resolved Redis Streams connection options.
        /// </summary>
        private readonly RedisStreamsOptions _options;

        /// <summary>
        /// Logger used for diagnostics and error reporting.
        /// </summary>
        private readonly ILogger<RedisStreamsClient> _logger;

        /// <summary>
        /// StackExchange.Redis multiplexer, or <c>null</c> until <see cref="ConnectAsync"/> succeeds.
        /// </summary>
        private IConnectionMultiplexer? _connection;

        /// <summary>
        /// Database used for stream operations, or <c>null</c> until connected.
        /// </summary>
        private IDatabase? _database;

        /// <summary>
        /// Fully-qualified consumer group name (app name + configured suffix).
        /// </summary>
        private string? _consumerGroup;

        /// <summary>
        /// Linked cancellation source for the active consume loop.
        /// </summary>
        private CancellationTokenSource? _linkedCts;

        /// <summary>
        /// Background task that reads and dispatches stream messages.
        /// </summary>
        private Task? _consumeLoop;

        /// <summary>
        /// Indicates whether this instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisStreamsClient"/> class.
        /// </summary>
        /// <param name="options">Redis Streams options monitored via <see cref="IOptions{TOptions}"/>.</param>
        /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
        public RedisStreamsClient(IOptions<RedisStreamsOptions> options, ILogger<RedisStreamsClient>? logger = null)
        {
            _options = options.Value;
            _logger = logger ?? NullLogger<RedisStreamsClient>.Instance;
        }

        /// <inheritdoc />
        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            if (_connection != null)
                return Task.CompletedTask;

            _connection = ConnectionMultiplexer.Connect(_options.ToConfigurationOptions());
            _database = _connection.GetDatabase(_options.DefaultDatabase);

            var appName = _options.AppName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "EasyCore";
            _consumerGroup = $"{appName}.{_options.ConsumerGroup}";
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task PublishAsync(
            string streamKey,
            string typeName,
            string payloadJson,
            RedisStreamHeader? header = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(streamKey);
            ArgumentException.ThrowIfNullOrEmpty(typeName);
            ArgumentException.ThrowIfNullOrEmpty(payloadJson);

            await ConnectAsync(cancellationToken).ConfigureAwait(false);

            header ??= new RedisStreamHeader();
            var entries = new NameValueEntry[]
            {
                new(RedisStreamMessageFormat.HeaderField, RedisStreamMessageFormat.SerializeHeader(header)),
                new(RedisStreamMessageFormat.TypeField, typeName),
                new(RedisStreamMessageFormat.PayloadField, payloadJson)
            };

            var streamId = await _database!.StreamAddAsync(streamKey, entries).ConfigureAwait(false);
            if (streamId.IsNullOrEmpty)
                throw new InvalidOperationException($"Redis Streams publish to '{streamKey}' failed.");
        }

        /// <inheritdoc />
        public async Task SubscribeAsync(
            IEnumerable<string> streamKeys,
            Func<RedisStreamsDeliveredMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(handler);
            await ConnectAsync(cancellationToken).ConfigureAwait(false);

            var streams = streamKeys.Distinct(StringComparer.Ordinal).ToList();
            if (streams.Count == 0)
            {
                _logger.LogWarning("Redis Streams SubscribeAsync called with no stream keys.");
                return;
            }

            foreach (var stream in streams)
            {
                await EnsureStreamAsync(stream).ConfigureAwait(false);
                await EnsureConsumerGroupAsync(stream).ConfigureAwait(false);
            }

            var positions = streams.Select(s => new StreamPosition(s, StreamPosition.NewMessages)).ToArray();
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _linkedCts.Token;

            _consumeLoop = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var readTasks = _database!.StreamReadGroupAsync(positions, _consumerGroup!, _consumerGroup!, 1);
                        var readSet = await Task.WhenAll(readTasks).ConfigureAwait(false);
                        var messages = readSet.SelectMany(set => set).ToList();

                        if (messages.Count == 0)
                        {
                            await Task.Delay(200, ct).ConfigureAwait(false);
                            continue;
                        }

                        foreach (var msg in messages)
                        {
                            if (msg.Entries.Length == 0)
                                continue;

                            var entry = msg.Entries[0];
                            var values = entry.Values.ToDictionary(v => (string)v.Name!, v => (string)v.Value!, StringComparer.Ordinal);

                            if (!values.TryGetValue(RedisStreamMessageFormat.HeaderField, out var headerJson)
                                || !values.TryGetValue(RedisStreamMessageFormat.TypeField, out var typeName)
                                || !values.TryGetValue(RedisStreamMessageFormat.PayloadField, out var payload))
                            {
                                _logger.LogWarning("Skipping malformed Redis Streams entry {Id} on {Stream}.", entry.Id, msg.Key);
                                continue;
                            }

                            var header = RedisStreamMessageFormat.DeserializeHeader(headerJson) ?? new RedisStreamHeader();
                            var delivered = new RedisStreamsDeliveredMessage
                            {
                                StreamKey = msg.Key!,
                                MessageId = entry.Id!,
                                TypeName = typeName,
                                PayloadJson = payload,
                                Header = header
                            };

                            await handler(delivered, ct).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Redis Streams consume loop error.");
                        await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }, ct);

            _logger.LogInformation("Redis Streams subscribed to {Count} streams.", streams.Count);
        }

        /// <inheritdoc />
        public async Task AcknowledgeAsync(string streamKey, string messageId, CancellationToken cancellationToken = default)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
            await _database!.StreamAcknowledgeAsync(streamKey, _consumerGroup!, messageId).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures the stream key exists by adding a bootstrap entry when the key is missing.
        /// </summary>
        /// <param name="streamKey">Stream key to create if absent.</param>
        /// <returns>A task that completes when the stream is known to exist.</returns>
        private async Task EnsureStreamAsync(string streamKey)
        {
            if (await _database!.KeyExistsAsync(streamKey).ConfigureAwait(false))
                return;

            await _database.StreamAddAsync(streamKey, "status", "created").ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures the consumer group exists on the stream, creating it for new messages when needed.
        /// </summary>
        /// <param name="streamKey">Stream key on which to create or verify the consumer group.</param>
        /// <returns>A task that completes when the consumer group is available.</returns>
        private async Task EnsureConsumerGroupAsync(string streamKey)
        {
            try
            {
                var groupInfo = await _database!.StreamGroupInfoAsync(streamKey).ConfigureAwait(false);
                if (groupInfo.Any(g => g.Name == _consumerGroup))
                    return;

                await _database.StreamCreateConsumerGroupAsync(streamKey, _consumerGroup, StreamPosition.NewMessages)
                    .ConfigureAwait(false);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Consumer group {Group} already exists.", _consumerGroup);
            }
        }

        /// <summary>
        /// Cancels the consume loop, closes the Redis connection, and releases resources.
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

            if (_connection != null)
            {
                await _connection.CloseAsync().ConfigureAwait(false);
                _connection.Dispose();
            }

            _linkedCts?.Dispose();
        }
    }
}
