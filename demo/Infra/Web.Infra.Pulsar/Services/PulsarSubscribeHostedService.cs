using System.Text;
using EasyCore.Pulsar;

namespace Web.Infra.Pulsar.Services;

/// <summary>
/// Connects and subscribes to the demo Pulsar topic; acknowledges after logging.
/// </summary>
public sealed class PulsarSubscribeHostedService : BackgroundService
{
    public const string DemoTopic = "easycore-infra-demo";

    private readonly IPulsarClient _client;
    private readonly ILogger<PulsarSubscribeHostedService> _logger;

    public PulsarSubscribeHostedService(IPulsarClient client, ILogger<PulsarSubscribeHostedService> logger)
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
                    "[Pulsar] Received topic={Topic} messageId={MessageId} body={Body}",
                    msg.Topic,
                    msg.MessageId,
                    body);

                await _client.AcknowledgeAsync(msg.MessageId, ct);
            },
            cancellationToken: stoppingToken);

        _logger.LogInformation("Pulsar subscribe loop started for topic {Topic}.", DemoTopic);

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
