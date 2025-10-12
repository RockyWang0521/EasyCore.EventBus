using EasyCore.EventBus.Event;

namespace Web.Kafka.Publish
{
    public class WebEventMessage2 : IEvent
    {
        public string? Message { get; set; }
    }
}
