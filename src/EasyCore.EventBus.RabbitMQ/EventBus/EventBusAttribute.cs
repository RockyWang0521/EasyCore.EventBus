namespace EasyCore.EventBus.RabbitMQ.EventBus
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class EventBusAttribute : Attribute
    {

        public EventBusAttribute(string topicName)
        {
        }
    }
}
