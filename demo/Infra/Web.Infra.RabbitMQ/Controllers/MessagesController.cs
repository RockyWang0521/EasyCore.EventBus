using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using EasyCore.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using Web.Infra.RabbitMQ.Services;

namespace Web.Infra.RabbitMQ.Controllers;

[ApiController]
[Route("api/messages")]
public sealed class MessagesController : ControllerBase
{
    private readonly IRabbitMQClient _client;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IRabbitMQClient client, ILogger<MessagesController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Publish a text payload to a routing key (default: infra.demo.ping).</summary>
    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request, CancellationToken ct)
    {
        var routingKey = string.IsNullOrWhiteSpace(request.RoutingKey)
            ? RabbitMqSubscribeHostedService.PingRoutingKey
            : request.RoutingKey!;

        var payload = JsonSerializer.Serialize(new
        {
            text = request.Text ?? "hello from EasyCore.RabbitMQ",
            at = DateTimeOffset.UtcNow
        });

        await _client.ConnectAsync(ct);
        await _client.PublishAsync(
            routingKey,
            Encoding.UTF8.GetBytes(payload),
            headers: new Dictionary<string, object>
            {
                ["x-demo"] = "Web.Infra.RabbitMQ",
                ["x-correlation-id"] = Guid.NewGuid().ToString("N")
            },
            cancellationToken: ct);

        _logger.LogInformation("Published to {RoutingKey}: {Payload}", routingKey, payload);
        return Ok(new { routingKey, payload });
    }

    /// <summary>Quick ping endpoint.</summary>
    [HttpPost("ping")]
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        return await Publish(new PublishRequest { Text = "ping", RoutingKey = RabbitMqSubscribeHostedService.PingRoutingKey }, ct);
    }
}

public sealed class PublishRequest
{
    /// <summary>Message text; wrapped into JSON with timestamp.</summary>
    public string? Text { get; set; }

    /// <summary>Optional routing key. Defaults to infra.demo.ping.</summary>
    [MaxLength(200)]
    public string? RoutingKey { get; set; }
}
