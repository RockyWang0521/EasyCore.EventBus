using RabbitMQ.Client;

namespace EasyCore.EventBus.RabbitMQ
{
    public interface IConnectionChannel
    {
        /// <summary>
        /// Get RabbitMQ Connection
        /// </summary>
        /// <returns></returns>
        IConnection GetConnection(IConnection? connection);

        /// <summary>
        /// Create RabbitMQ Model
        /// </summary>
        /// <returns></returns>
        IModel CreateModel(IConnection? connection);
    }
}
