using EasyCore.EventBus.Event;

namespace Web.Pulsar.Publish
{
    public class WebEventMessage4 : IEvent
    {
        public string? Message { get; set; }
    }
}
