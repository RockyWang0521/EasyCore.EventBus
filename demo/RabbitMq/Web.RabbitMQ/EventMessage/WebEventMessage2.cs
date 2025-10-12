using EasyCore.EventBus.Event;

namespace Web.RabbitMQ
{
    public class WebEventMessage2 : IEvent
    {
        public string? Message { get; set; }
    }
}
