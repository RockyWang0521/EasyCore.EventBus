using RabbitMQ.Client;

namespace EasyCore.EventBus.RabbitMQ
{
    public interface IConnectionChannel
    {
        IConnection GetConnection();

        IModel CreateModel();
    }
}
