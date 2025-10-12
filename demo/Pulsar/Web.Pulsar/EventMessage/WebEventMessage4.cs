using EasyCore.EventBus.Event;

namespace Web.Pulsar
{
    public class WebEventMessage4 : IEvent
    {
        public string? Message { get; set; }
    }
}
