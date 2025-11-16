using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace EasyCore.Kafka
{
    /// <summary>
    /// Default <see cref="IKafkaClient"/> implementation.
    /// </summary>
    public sealed class KafkaClient : IKafkaClient
    {
        private readonly KafkaOptions _options;
        private readonly ILogger<KafkaClient> _logger;
        private IConsumer<string, byte[]>? _consumer;
        private CancellationTokenSource? _linkedCts;
        private Task? _consumeLoop;
        private bool _disposed;

        public KafkaClient(IOptions<KafkaOptions> options, ILogger<KafkaClient>? logger = null)
        {
            _options = options.Value;
            _logger = logger ?? NullLogger<KafkaClient>.Instance;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_consumer != null)
                return Task.CompletedTask;

            var appName = _options.AppName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "EasyCore";
            var config = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.RoundRobin,
                GroupId = $"{appName}.{_options.GroupId}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = true,
                EnableAutoCommit = false,
                LogConnectionClose = false,
            };

            _consumer = new ConsumerBuilder<string, byte[]>(config).Build();
            return Task.CompletedTask;
        }

        public async Task PublishAsync(
            string topic,
            ReadOnlyMemory<byte> body,
            string? key = null,
            IDictionary<string, byte[]>? headers = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(topic);
            ObjectDisposedException.ThrowIf(_disposed, this);

            var config = new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                QueueBufferingMaxMessages = _options.QueueBufferingMaxMessages,
                MessageTimeoutMs = _options.MessageTimeoutMs,
                RequestTimeoutMs = _options.RequestTimeoutMs,
            };

            using var producer = new ProducerBuilder<string, byte[]>(config).Build();

            var message = new Message<string, byte[]>
            {
                Key = key ?? string.Empty,
                Value = body.ToArray()
            };

            if (headers != null)
            {
                message.Headers = new Headers();
                foreach (var header in headers)
                    message.Headers.Add(header.Key, header.Value);
            }

            var result = await producer.ProduceAsync(topic, message, cancellationToken).ConfigureAwait(false);
            if (result.Status is not (PersistenceStatus.Persisted or PersistenceStatus.PossiblyPersisted))
                throw new InvalidOperationException($"Kafka publish to topic '{topic}' did not persist.");
        }

        public async Task EnsureTopicsAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default)
        {
            var topicList = topics.Distinct(StringComparer.Ordinal).ToList();
            if (topicList.Count == 0)
                return;

            var config = new AdminClientConfig { BootstrapServers = _options.BootstrapServers };
            using var adminClient = new AdminClientBuilder(config).Build();

            var specs = topicList.Select(name => new TopicSpecification { Name = name }).ToList();
            try
            {
                await adminClient.CreateTopicsAsync(specs).ConfigureAwait(false);
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                _logger.LogDebug("Kafka topics already exist.");
            }
            catch (CreateTopicsException ex)
            {
                var nonExists = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
                if (nonExists.Count > 0)
                    throw;
            }
        }

        public async Task SubscribeAsync(
            IEnumerable<string> topics,
            Func<KafkaDeliveredMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(handler);
            await ConnectAsync(cancellationToken).ConfigureAwait(false);

            var topicList = topics.Distinct(StringComparer.Ordinal).ToList();
            if (topicList.Count == 0)
            {
                _logger.LogWarning("Kafka SubscribeAsync called with no topics.");
                return;
            }

            await EnsureTopicsAsync(topicList, cancellationToken).ConfigureAwait(false);
            _consumer!.Subscribe(topicList);

            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _linkedCts.Token;

            _consumeLoop = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    ConsumeResult<string, byte[]>? result = null;
                    try
                    {
                        result = _consumer.Consume(TimeSpan.FromSeconds(1));
                        if (result == null || result.IsPartitionEOF || result.Message?.Value == null)
                            continue;

                        var headerMap = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                        if (result.Message.Headers != null)
                        {
                            foreach (var header in result.Message.Headers)
                                headerMap[header.Key] = header.GetValueBytes();
                        }

                        var delivered = new KafkaDeliveredMessage
                        {
                            Topic = result.Topic,
                            Key = result.Message.Key,
                            Body = result.Message.Value,
                            Headers = headerMap,
                            NativeResult = result
                        };

                        await handler(delivered, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Kafka consume loop error on topic {Topic}.", result?.Topic);
                    }
                }
            }, ct);

            _logger.LogInformation("Kafka subscribed to {Count} topics.", topicList.Count);
        }

        public void Commit(object nativeResult)
        {
            if (_consumer == null)
                return;

            if (nativeResult is ConsumeResult<string, byte[]> result)
                _consumer.Commit(result);
        }

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

            _consumer?.Close();
            _consumer?.Dispose();
            _linkedCts?.Dispose();
        }
    }
}
