using EasyCore.EventBus.Event;

namespace Web.RabbitMQ.Publish
{
    public class WebEventMessage2 : IEvent
    {
        public string? Message { get; set; }
    }
}
