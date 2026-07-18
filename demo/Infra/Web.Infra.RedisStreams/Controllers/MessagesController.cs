using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EasyCore.RedisStreams;
using Microsoft.AspNetCore.Mvc;
using Web.Infra.RedisStreams.Services;

namespace Web.Infra.RedisStreams.Controllers;

[ApiController]
[Route("api/messages")]
public sealed class MessagesController : ControllerBase
{
    private readonly IRedisStreamsClient _client;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IRedisStreamsClient client, ILogger<MessagesController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request, CancellationToken ct)
    {
        var stream = string.IsNullOrWhiteSpace(request.StreamKey)
            ? RedisSubscribeHostedService.DemoStream
            : request.StreamKey!;

        var typeName = string.IsNullOrWhiteSpace(request.TypeName)
            ? RedisSubscribeHostedService.DemoType
            : request.TypeName!;

        var payload = JsonSerializer.Serialize(new
        {
            text = request.Text ?? "hello from EasyCore.RedisStreams",
            at = DateTimeOffset.UtcNow
        });

        await _client.ConnectAsync(ct);
        await _client.PublishAsync(
            stream,
            typeName,
            payload,
            header: new RedisStreamHeader { RetryCount = 0, RetryInterval = 0 },
            cancellationToken: ct);

        _logger.LogInformation("Published to {Stream} type={Type}: {Payload}", stream, typeName, payload);
        return Ok(new { stream, typeName, payload });
    }

    [HttpPost("ping")]
    public async Task<IActionResult> Ping(CancellationToken ct)
        => await Publish(new PublishRequest { Text = "ping" }, ct);
}

public sealed class PublishRequest
{
    public string? Text { get; set; }

    [MaxLength(200)]
    public string? StreamKey { get; set; }

    [MaxLength(200)]
    public string? TypeName { get; set; }
}
