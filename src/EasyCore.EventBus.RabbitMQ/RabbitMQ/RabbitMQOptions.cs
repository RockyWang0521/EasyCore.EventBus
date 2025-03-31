namespace EasyCore.EventBus.RabbitMQ
{
    public class RabbitMQOptions
    {
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
        /// 交换机类型
        /// </summary>
        public  string ExchangeType = "topic";

        /// <summary>
        /// 虚拟主机
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// 交换机名称
        /// </summary>
        public string ExchangeName { get; set; } = "event_bus";

        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; set; } = "event_bus_queue";

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
    }
}
