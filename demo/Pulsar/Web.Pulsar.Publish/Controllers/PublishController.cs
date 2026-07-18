using EasyCore.EventBus.Distributed;
using Microsoft.AspNetCore.Mvc;

namespace Web.Pulsar.Publish.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PublishController : ControllerBase
{
    private readonly IDistributedEventBus _bus;
    private readonly ILogger<PublishController> _logger;

    public PublishController(IDistributedEventBus bus, ILogger<PublishController> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PublishAll([FromBody] PublishRequest? request)
    {
        var text = request?.Message ?? $"Hello Pulsar @ {DateTimeOffset.Now:O}";
        var published = new List<string>();

        await PublishAsync(new WebEventMessage { Message = text }, published);
        await PublishAsync(new WebEventMessage2 { Message = text }, published);
        await PublishAsync(new WebEventMessage3 { Message = text }, published);
        await PublishAsync(new WebEventMessage4 { Message = text }, published);

        _logger.LogInformation("Published {Count} events", published.Count);
        return Ok(new { message = text, published });
    }

    [HttpPost("one")]
    public async Task<IActionResult> PublishOne([FromBody] PublishRequest? request)
    {
        var text = request?.Message ?? $"single @ {DateTimeOffset.Now:O}";
        await _bus.PublishAsync(new WebEventMessage { Message = text });
        return Ok(new { type = nameof(WebEventMessage), message = text });
    }

    [HttpPost("batch")]
    public async Task<IActionResult> PublishBatch([FromBody] BatchPublishRequest? request)
    {
        var count = Math.Clamp(request?.Count ?? 10, 1, 1000);
        var text = request?.Message ?? "batch";
        for (var i = 0; i < count; i++)
            await _bus.PublishAsync(new WebEventMessage2 { Message = $"{text}#{i}" });

        return Ok(new { count, message = text });
    }

    private async Task PublishAsync<T>(T evt, List<string> published) where T : class, EasyCore.EventBus.Event.IEvent
    {
        await _bus.PublishAsync(evt);
        published.Add(typeof(T).Name);
    }
}

public sealed class PublishRequest
{
    public string? Message { get; set; }
}

public sealed class BatchPublishRequest
{
    public string? Message { get; set; }
    public int Count { get; set; } = 10;
}
