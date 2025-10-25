using EasyCore.EventBus.Event;

namespace Web.Redis
{
    public class WebEventMessage2 : IEvent
    {
        public string? Message { get; set; }
    }
}
