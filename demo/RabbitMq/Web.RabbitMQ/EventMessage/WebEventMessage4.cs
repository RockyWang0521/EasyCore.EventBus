using EasyCore.EventBus.Event;

namespace Web.RabbitMQ
{
    public class WebEventMessage4 : IEvent
    {
        public string? Message { get; set; }
    }
}
