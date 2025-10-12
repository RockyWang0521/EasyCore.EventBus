using EasyCore.EventBus.Event;

namespace Web.Pulsar
{
    public class WebEventMessage3 : IEvent
    {
        public string? Message { get; set; }
    }
}
