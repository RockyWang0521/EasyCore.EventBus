using EasyCore.EventBus.Event;

namespace Web.Redis.Publish
{
    public class MyEventMessage : IDistributedEventHandler<WebEventMessage>
    {
        private readonly ILogger<MyEventMessage> _logger;

        public MyEventMessage(ILogger<MyEventMessage> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(WebEventMessage eventMessage)
        {
            _logger.LogInformation($"Received event message: {eventMessage.Message}--{Guid.NewGuid()}");

            await Task.CompletedTask;
        }
    }
}
