using EasyCore.EventBus.Event;

namespace Web.RabbitMQ.Publish
{
    public class WebEventMessage4 : IEvent
    {
        public string? Message { get; set; }
    }
}
