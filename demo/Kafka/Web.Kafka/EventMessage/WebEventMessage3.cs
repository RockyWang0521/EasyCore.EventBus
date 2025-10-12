using EasyCore.EventBus.Event;

namespace Web.Kafka
{
    public class WebEventMessage3 : IEvent
    {
        public string? Message { get; set; }
    }
}
