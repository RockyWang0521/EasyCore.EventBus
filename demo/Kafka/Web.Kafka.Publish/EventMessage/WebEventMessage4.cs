using EasyCore.EventBus.Event;

namespace Web.Kafka.Publish
{
    public class WebEventMessage4 : IEvent
    {
        public string? Message { get; set; }
    }
}
