using StackExchange.Redis;

namespace EasyCore.RedisStreams
{
    /// <summary>
    /// Redis Streams connection and consumer group options.
    /// </summary>
    public class RedisStreamsOptions
    {
        /// <summary>
        /// Redis endpoints, e.g. <c>localhost:6379</c>.
        /// </summary>
        public List<string> EndPoints { get; set; } = new();

        /// <summary>
        /// Redis user name for ACL authentication, when required.
        /// </summary>
        public string? User { get; set; }

        /// <summary>
        /// Redis password for authentication.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Connect timeout in seconds (converted to milliseconds for StackExchange.Redis).
        /// </summary>
        public int ConnectTimeout { get; set; } = 10;

        /// <summary>
        /// Sync timeout in seconds (converted to milliseconds for StackExchange.Redis).
        /// </summary>
        public int SyncTimeout { get; set; } = 10;

        /// <summary>
        /// Whether to abort when the initial connect fails.
        /// </summary>
        public bool AbortOnConnectFail { get; set; } = false;

        /// <summary>
        /// Default Redis database index.
        /// </summary>
        public int DefaultDatabase { get; set; } = 0;

        /// <summary>
        /// Consumer group name suffix. Combined with <see cref="AppName"/> to form the full group name.
        /// </summary>
        public string ConsumerGroup { get; set; } = "RedisGroup";

        /// <summary>
        /// Application name used for consumer group naming.
        /// When null, the entry assembly name is used.
        /// </summary>
        public string? AppName { get; set; }

        /// <summary>
        /// Converts these options to a StackExchange.Redis <see cref="ConfigurationOptions"/> instance.
        /// </summary>
        /// <returns>A <see cref="ConfigurationOptions"/> instance for connecting with StackExchange.Redis.</returns>
        public ConfigurationOptions ToConfigurationOptions()
        {
            var configOptions = new ConfigurationOptions
            {
                User = User,
                Password = Password,
                ConnectTimeout = ConnectTimeout * 1000,
                SyncTimeout = SyncTimeout * 1000,
                AbortOnConnectFail = AbortOnConnectFail,
                DefaultDatabase = DefaultDatabase
            };

            foreach (var endpoint in EndPoints)
            {
                if (!string.IsNullOrWhiteSpace(endpoint))
                    configOptions.EndPoints.Add(endpoint);
            }

            return configOptions;
        }
    }
}
