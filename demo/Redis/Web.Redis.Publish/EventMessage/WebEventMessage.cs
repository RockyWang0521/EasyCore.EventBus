using EasyCore.EventBus.Event;

namespace Web.Redis.Publish
{
    public class WebEventMessage : IEvent
    {
        public string? Message { get; set; }
    }
}
