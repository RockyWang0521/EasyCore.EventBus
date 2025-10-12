using EasyCore.EventBus.Event;

namespace Web.RabbitMQ
{
    public class MyEventMessage3 : IDistributedEventHandler<WebEventMessage3>
    {
        private readonly ILogger<MyEventMessage3> _logger;

        public MyEventMessage3(ILogger<MyEventMessage3> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(WebEventMessage3 eventMessage)
        {
            _logger.LogInformation($"Received event message: {eventMessage.Message}--{Guid.NewGuid()}");

            await Task.CompletedTask;
        }
    }
}
