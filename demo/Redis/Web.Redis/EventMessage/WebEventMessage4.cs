using EasyCore.EventBus.Event;

namespace Web.Redis
{
    public class WebEventMessage4 : IEvent
    {
        public string? Message { get; set; }
    }
}
