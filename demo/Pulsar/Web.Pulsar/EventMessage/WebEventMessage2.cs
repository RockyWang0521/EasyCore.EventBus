using EasyCore.EventBus.Event;

namespace Web.Pulsar
{
    public class WebEventMessage2 : IEvent
    {
        public string? Message { get; set; }
    }
}
