using EasyCore.EventBus.Event;

namespace Web.Kafka
{
    public class WebEventMessage4 : IEvent
    {
        public string? Message { get; set; }
    }
}
