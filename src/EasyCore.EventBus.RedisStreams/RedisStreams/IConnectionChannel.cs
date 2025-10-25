using StackExchange.Redis;

namespace EasyCore.EventBus.RedisStreams
{
    public interface IConnectionChannel
    {
        IDatabase GetDatabase(IConnectionMultiplexer connection);

        IConnectionMultiplexer GetConnection();
    }
}
