using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EasyCore.EventBus.RedisStreams
{
    public class ConnectionChannel : IConnectionChannel
    {
        private readonly RedisStreamsOptions _options;

        public ConnectionChannel(IOptions<RedisStreamsOptions> options) => _options = options.Value;

        public IDatabase GetDatabase(IConnectionMultiplexer connection) => connection.GetDatabase(_options.DefaultDatabase);

        public IConnectionMultiplexer GetConnection()
        {
            var configurationOptions = _options.ToConfigurationOptions();

            return ConnectionMultiplexer.Connect(configurationOptions);
        }
    }

    public class RedisHeader
    {
        /// <summary>
        /// Failure retry count
        /// </summary>
        public int? RetryCount { get; set; }

        /// <summary>
        /// Failure retry interval (seconds)
        /// </summary>
        public int? RetryInterval { get; set; }
    }
}
