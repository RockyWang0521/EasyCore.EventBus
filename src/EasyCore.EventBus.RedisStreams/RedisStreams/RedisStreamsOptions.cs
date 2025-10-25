using StackExchange.Redis;

namespace EasyCore.EventBus.RedisStreams
{
    public class RedisStreamsOptions
    {
        /// <summary>
        /// Redis连接配置
        /// </summary>
        public List<string> EndPoints { get; set; } = new List<string>();

        /// <summary>
        /// Redis用户名
        /// </summary>
        public string? User = default;

        /// <summary>
        /// Redis密码
        /// </summary>
        public string? Password = default;

        /// <summary>
        /// 连接超时时间(S)
        /// </summary>
        public int ConnectTimeout = 10;

        /// <summary>
        /// 同步超时时间(S)
        /// </summary>
        public int SyncTimeout = 10;

        /// <summary>
        /// 是否在连接失败时中止
        /// </summary>
        public bool AbortOnConnectFail = false;

        /// <summary>
        /// 默认数据库
        /// </summary>
        public int DefaultDatabase = 0;

        /// <summary>
        /// 转换为 Redis 配置
        /// </summary>
        public ConfigurationOptions ToConfigurationOptions()
        {
            var configOptions = new ConfigurationOptions
            {
                User = this.User,
                Password = this.Password,
                ConnectTimeout = this.ConnectTimeout * 1000,
                SyncTimeout = this.SyncTimeout * 1000,
                AbortOnConnectFail = this.AbortOnConnectFail,
                DefaultDatabase = this.DefaultDatabase
            };

            foreach (var endpoint in this.EndPoints)
            {
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    configOptions.EndPoints.Add(endpoint);
                }
            }

            return configOptions;
        }
    }
}
