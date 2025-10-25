using EasyCore.EventBus.Event;

namespace Web.Redis
{
    public class WebEventMessage3 : IEvent
    {
        public string? Message { get; set; }
    }
}
