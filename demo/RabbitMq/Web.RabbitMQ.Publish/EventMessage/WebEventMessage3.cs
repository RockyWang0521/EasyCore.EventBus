using EasyCore.EventBus.Event;

namespace Web.RabbitMQ.Publish
{
    public class WebEventMessage3 : IEvent
    {
        public string? Message { get; set; }
    }
}
