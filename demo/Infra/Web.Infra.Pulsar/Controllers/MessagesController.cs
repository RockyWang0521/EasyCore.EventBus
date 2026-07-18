using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using EasyCore.Pulsar;
using Microsoft.AspNetCore.Mvc;
using Web.Infra.Pulsar.Services;

namespace Web.Infra.Pulsar.Controllers;

[ApiController]
[Route("api/messages")]
public sealed class MessagesController : ControllerBase
{
    private readonly IPulsarClient _client;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IPulsarClient client, ILogger<MessagesController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request, CancellationToken ct)
    {
        var topic = string.IsNullOrWhiteSpace(request.Topic)
            ? PulsarSubscribeHostedService.DemoTopic
            : request.Topic!;

        var payload = JsonSerializer.Serialize(new
        {
            text = request.Text ?? "hello from EasyCore.Pulsar",
            at = DateTimeOffset.UtcNow
        });

        await _client.ConnectAsync(ct);
        await _client.PublishAsync(
            topic,
            Encoding.UTF8.GetBytes(payload),
            properties: new Dictionary<string, string>
            {
                ["x-demo"] = "Web.Infra.Pulsar",
                ["EventType"] = topic
            },
            cancellationToken: ct);

        _logger.LogInformation("Published to {Topic}: {Payload}", topic, payload);
        return Ok(new { topic, payload });
    }

    [HttpPost("ping")]
    public async Task<IActionResult> Ping(CancellationToken ct)
        => await Publish(new PublishRequest { Text = "ping" }, ct);
}

public sealed class PublishRequest
{
    public string? Text { get; set; }

    [MaxLength(200)]
    public string? Topic { get; set; }
}
