using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using EasyCore.Kafka;
using Microsoft.AspNetCore.Mvc;
using Web.Infra.Kafka.Services;

namespace Web.Infra.Kafka.Controllers;

[ApiController]
[Route("api/messages")]
public sealed class MessagesController : ControllerBase
{
    private readonly IKafkaClient _client;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IKafkaClient client, ILogger<MessagesController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request, CancellationToken ct)
    {
        var topic = string.IsNullOrWhiteSpace(request.Topic)
            ? KafkaSubscribeHostedService.DemoTopic
            : request.Topic!;

        var payload = JsonSerializer.Serialize(new
        {
            text = request.Text ?? "hello from EasyCore.Kafka",
            at = DateTimeOffset.UtcNow
        });

        var headers = new Dictionary<string, byte[]>
        {
            ["x-demo"] = Encoding.UTF8.GetBytes("Web.Infra.Kafka")
        };

        await _client.PublishAsync(
            topic,
            Encoding.UTF8.GetBytes(payload),
            key: request.Key ?? Guid.NewGuid().ToString("N"),
            headers: headers,
            cancellationToken: ct);

        _logger.LogInformation("Published to {Topic}: {Payload}", topic, payload);
        return Ok(new { topic, payload, key = request.Key });
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

    public string? Key { get; set; }
}
