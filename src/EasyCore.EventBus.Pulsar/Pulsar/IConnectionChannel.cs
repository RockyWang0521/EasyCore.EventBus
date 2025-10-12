using Pulsar.Client.Api;

namespace EasyCore.EventBus.Pulsar
{
    public interface IConnectionChannel
    {
        /// <summary>
        /// Create a new Pulsar client instance.
        /// </summary>
        /// <param name="pulsarClient">Pulsar client</param>
        /// <returns></returns>
        Task<PulsarClient> PulsarClientAsync(PulsarClient? pulsarClient);

        /// <summary>
        /// Create a new Pulsar producer instance.
        /// </summary>
        /// <param name="topic">topic</param>
        /// <param name="pulsarClient">Pulsar client</param>
        /// <returns></returns>
        Task<IProducer<byte[]>> PulsarProducerAsync(string topic, PulsarClient? pulsarClient);

        /// <summary>
        /// Create a new Pulsar consumer instance.
        /// </summary>
        /// <param name="pulsarClient">Pulsar client</param>
        /// <returns></returns>
        Task<IConsumer<byte[]>?> PulsarConsumerAsync(PulsarClient? pulsarClient);

        /// <summary>
        /// Create a  Pulsar Topic.
        /// </summary>
        /// <returns></returns>
        List<Type> CreateTopic();
    }
}
