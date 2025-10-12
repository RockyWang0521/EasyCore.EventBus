using EasyCore.EventBus.Event;

namespace Web.RabbitMQ.Publish
{
    public class WebEventMessage : IEvent
    {
        public string? Message { get; set; }
    }
}
