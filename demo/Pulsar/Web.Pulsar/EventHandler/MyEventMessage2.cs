using EasyCore.EventBus.Event;

namespace Web.Pulsar
{
    public class MyEventMessage2 : IDistributedEventHandler<WebEventMessage2>
    {
        private readonly ILogger<MyEventMessage2> _logger;

        public MyEventMessage2(ILogger<MyEventMessage2> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(WebEventMessage2 eventMessage)
        {
            _logger.LogInformation($"Received event message: {eventMessage.Message}--{Guid.NewGuid()}");

            await Task.CompletedTask;
        }
    }
}
