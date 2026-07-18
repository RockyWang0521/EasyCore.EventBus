using System.Text;
using EasyCore.RabbitMQ;

namespace Web.Infra.RabbitMQ.Services;

/// <summary>
/// Connects and subscribes to demo routing keys on startup; logs and acks each message.
/// </summary>
public sealed class RabbitMqSubscribeHostedService : BackgroundService
{
    public const string PingRoutingKey = "infra.demo.ping";
    public const string EchoRoutingKey = "infra.demo.echo";

    private readonly IRabbitMQClient _client;
    private readonly ILogger<RabbitMqSubscribeHostedService> _logger;

    public RabbitMqSubscribeHostedService(IRabbitMQClient client, ILogger<RabbitMqSubscribeHostedService> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync(stoppingToken);

        await _client.SubscribeAsync(
            routingKeys: new[] { PingRoutingKey, EchoRoutingKey },
            handler: async (msg, ct) =>
            {
                var body = Encoding.UTF8.GetString(msg.Body.Span);
                _logger.LogInformation(
                    "[RabbitMQ] Received routingKey={RoutingKey} deliveryTag={Tag} body={Body}",
                    msg.RoutingKey,
                    msg.DeliveryTag,
                    body);

                _client.Ack(msg.DeliveryTag);
                await Task.CompletedTask;
            },
            cancellationToken: stoppingToken);

        _logger.LogInformation("RabbitMQ subscribe loop started for {Keys}.", string.Join(", ", PingRoutingKey, EchoRoutingKey));

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
