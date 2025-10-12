using EasyCore.EventBus.Event;

namespace Web.RabbitMQ
{
    public class MyEventMessage4 : IDistributedEventHandler<WebEventMessage4>
    {
        private readonly ILogger<MyEventMessage4> _logger;

        public MyEventMessage4(ILogger<MyEventMessage4> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(WebEventMessage4 eventMessage)
        {
            _logger.LogInformation($"Received event message: {eventMessage.Message}--{Guid.NewGuid()}");

            await Task.CompletedTask;
        }
    }
}
