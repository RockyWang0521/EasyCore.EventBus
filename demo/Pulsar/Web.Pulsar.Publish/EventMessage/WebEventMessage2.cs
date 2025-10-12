using EasyCore.EventBus.Event;

namespace Web.Pulsar.Publish
{
    public class WebEventMessage2 : IEvent
    {
        public string? Message { get; set; }
    }
}
