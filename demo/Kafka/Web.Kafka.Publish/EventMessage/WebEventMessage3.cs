using EasyCore.EventBus.Event;

namespace Web.Kafka.Publish
{
    public class WebEventMessage3 : IEvent
    {
        public string? Message { get; set; }
    }
}
