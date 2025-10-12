using Confluent.Kafka;

namespace EasyCore.EventBus.Kafka.Kafka
{
    public interface IConnectionChannel
    {
        /// <summary>
        /// Create a new producer to the Kafka cluster.
        /// </summary>
        /// <returns></returns>
        IProducer<string, string> CreateProducer(IProducer<string, string>? producer);

        /// <summary>
        /// Create a new producer to the Kafka cluster.
        /// </summary>
        /// <returns></returns>
        IProducer<string, string> CreateProducer();

        /// <summary>
        /// Close the Producer.
        /// </summary>
        /// <returns></returns>
        bool CloseProducer(IProducer<string, string>? producer);

        /// <summary>
        /// Create a new consumer to the Kafka cluster.
        /// </summary>
        /// <returns></returns>
        IConsumer<string, string> CreateConsumer(IConsumer<string, string>? producer);
    }
}
