using System.Text;
using EasyCore.Kafka;

namespace Web.Infra.Kafka.Services;

/// <summary>
/// Connects and subscribes to the demo Kafka topic; commits after logging each message.
/// </summary>
public sealed class KafkaSubscribeHostedService : BackgroundService
{
    public const string DemoTopic = "easycore.infra.demo";

    private readonly IKafkaClient _client;
    private readonly ILogger<KafkaSubscribeHostedService> _logger;

    public KafkaSubscribeHostedService(IKafkaClient client, ILogger<KafkaSubscribeHostedService> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync(stoppingToken);

        await _client.SubscribeAsync(
            topics: new[] { DemoTopic },
            handler: async (msg, ct) =>
            {
                var body = Encoding.UTF8.GetString(msg.Body.Span);
                _logger.LogInformation(
                    "[Kafka] Received topic={Topic} key={Key} body={Body}",
                    msg.Topic,
                    msg.Key,
                    body);

                if (msg.NativeResult != null)
                    _client.Commit(msg.NativeResult);

                await Task.CompletedTask;
            },
            cancellationToken: stoppingToken);

        _logger.LogInformation("Kafka subscribe loop started for topic {Topic}.", DemoTopic);

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
