using EasyCore.EventBus.Event;

namespace Web.Pulsar.Publish
{
    public class WebEventMessage : IEvent
    {
        public string? Message { get; set; }
    }
}
