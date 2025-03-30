namespace EasyCore.EventBus.RabbitMQ.EventBus
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class EventBusAttribute : Attribute
    {
        public string QueueName { get; }

        public EventBusAttribute(string queueName)
        {
            QueueName = queueName;
        }
    }
}
