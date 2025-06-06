using EasyCore.EventBus.Event;

namespace EasyCore.EventBus
{
    public class EventBusOptions
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public EventBusOptions()
        {
            Extensions = new List<IEventOptionsExtension>();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// RabbitMQ主机名
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// RabbitMQ用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// RabbitMQ密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// RabbitMQ端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 交换机名称
        /// </summary>
        public string ExchangeName { get; set; } = "EasyCore.EventBus";

        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; set; } = "EasyCore.Queue";

        /// <summary>
        /// 交换机类型
        /// </summary>
        public string ExchangeType { get; set; } = "topic";

        /// <summary>
        /// 虚拟主机
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// 失败重试次数
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// 失败重试间隔（秒）
        /// </summary>
        public int RetryInterval { get; set; } = 3;

        /// <summary>
        /// 失败回调
        /// </summary>
        public Action<string, string>? FailureCallback { get; set; }

        /// <summary>
        /// 获取或设置队列消息自动删除时间，默认10天（以毫秒为单位）
        /// </summary>
        public int MessageTTL { get; set; } = 864000000;

        /// <summary>
        ///  队列模式
        /// </summary>
        public string QueueMode { get; set; } = default!;

        /// <summary>
        /// 是否持久化
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// 是否排他
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// 是否自动删除
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// 队列类型
        /// </summary>
        public string QueueType { get; set; } = default!;

        /// <summary>
        /// 所有的服务
        /// </summary>
        internal IList<IEventOptionsExtension>? Extensions { get; }

#pragma warning disable CS8602 // Converting null literal or possible null value to non-nullable type.
        public void RegisterExtension(IEventOptionsExtension extension)
        {
            Extensions.Add(extension);
        }
#pragma warning restore CS8602 // Converting null literal or possible null value to non-nullable type.
    }
}
