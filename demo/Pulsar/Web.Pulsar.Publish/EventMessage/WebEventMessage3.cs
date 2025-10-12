using EasyCore.EventBus.Event;

namespace Web.Pulsar.Publish
{
    public class WebEventMessage3 : IEvent
    {
        public string? Message { get; set; }
    }
}
