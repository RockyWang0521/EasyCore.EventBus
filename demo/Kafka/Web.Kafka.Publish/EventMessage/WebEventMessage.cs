using EasyCore.EventBus.Event;

namespace Web.Kafka.Publish
{
    public class WebEventMessage : IEvent
    {
        public string? Message { get; set; }
    }
}
