using EasyCore.RedisStreams;

namespace Web.Infra.RedisStreams.Services;

/// <summary>
/// Connects and subscribes to the demo Redis stream; acks after logging.
/// </summary>
public sealed class RedisSubscribeHostedService : BackgroundService
{
    public const string DemoStream = "easycore:infra:demo";
    public const string DemoType = "Infra.Ping";

    private readonly IRedisStreamsClient _client;
    private readonly ILogger<RedisSubscribeHostedService> _logger;

    public RedisSubscribeHostedService(IRedisStreamsClient client, ILogger<RedisSubscribeHostedService> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync(stoppingToken);

        await _client.SubscribeAsync(
            streamKeys: new[] { DemoStream },
            handler: async (msg, ct) =>
            {
                _logger.LogInformation(
                    "[RedisStreams] Received stream={Stream} id={Id} type={Type} payload={Payload} retry={Retry}",
                    msg.StreamKey,
                    msg.MessageId,
                    msg.TypeName,
                    msg.PayloadJson,
                    msg.Header.RetryCount);

                await _client.AcknowledgeAsync(msg.StreamKey, msg.MessageId, ct);
            },
            cancellationToken: stoppingToken);

        _logger.LogInformation("Redis Streams subscribe loop started for {Stream}.", DemoStream);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }
}
