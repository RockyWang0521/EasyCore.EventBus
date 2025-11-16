# EasyCore.EventBus - .NET Core 事件总线解决方案 🚀

[English README](./README.en-US.md)

## 📋 项目介绍

EasyCore.EventBus 是一个专为 .NET Core 设计的轻量级事件总线库，帮助开发者轻松实现事件驱动架构（EDA）。该库支持多种消息队列作为事件传输媒介，提供了统一的事件发布-订阅接口，让不同组件、模块或服务之间的异步通信变得更加简单。

🎯 核心概念
事件总线（EventBus）
事件总线是事件驱动架构中的核心组件，它基于发布-订阅（Pub/Sub）模式，实现了系统各部分的解耦：

| 组件     | 角色    | 职责              |
|--------|-------|-----------------|
| 📤 发布者 | 事件生产者 | 将事件推送到 EventBus |
| 📥 订阅者 | 事件消费者 | 订阅并处理感兴趣的事件     |
| 📨 事件  | 消息载体  | 表示系统中的状态变化或行为   |

🔌 支持的消息队列
EasyCore.EventBus 提供了多种消息队列支持：

| 包名称                            | 消息队列          | 特性          |
|--------------------------------|---------------|-------------|
| EasyCore.EventBus.Kafka        | Apache Kafka  | 高吞吐量、分布式    |
| EasyCore.EventBus.Pulsar       | Apache Pulsar | 低延迟、云原生     |
| EasyCore.EventBus.RabbitMQ     | RabbitMQ      | 并发量高、AMQP协议 |
| EasyCore.EventBus.RedisStreams | Redis Streams | 内存级性能、简单易用  |


## 🚀 快速开始

### 1. 本地 EventBus（进程内通信）

#### WinForms 应用配置 🖥️

```
[STAThread]
static void Main()
{
    var host = CreateHostBuilder().Build();

    ApplicationConfiguration.Initialize();

    var mainForm = host.Services.GetRequiredService<Main>();
    var backgroundService = host.Services.GetRequiredService<IHostedService>();

    backgroundService.StartAsync(default).Wait();
    Application.Run(mainForm);
}

public static IHostBuilder CreateHostBuilder() =>
    Host.CreateDefaultBuilder()
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton<Main>();
            
            // 🎯 注册 EventBus 服务
            services.AddAppEventBus(options =>
            {
                options.RabbitMQ(opt =>
                {
                    opt.HostName = "192.168.157.142";
                    opt.UserName = "123";
                    opt.Password = "123";
                    opt.Port = 5672;
                });
            });
        });
```

#### Web API 配置 🌐

```
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // 🎯 注册 EventBus 服务
        builder.Services.AddAppEventBus(options =>
        {
            options.RabbitMQ(opt =>
            {
                opt.HostName = "192.168.157.142";
                opt.UserName = "123";
                opt.Password = "123";
                opt.Port = 5672;
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
```
### 2. 定义事件和处理器

#### 事件定义 📨

```
public class LocalEventMessage : IEvent
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
```
#### 事件处理器 ⚙️
```
public class MyLocalEventHandler : ILocalEventHandler<LocalEventMessage>
{
    private readonly ILogger<MyLocalEventHandler> _logger;

    public MyLocalEventHandler(ILogger<MyLocalEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(LocalEventMessage eventMessage)
    {
        // ✅ 处理事件逻辑
        _logger.LogInformation($"收到事件: {eventMessage.Message} at {eventMessage.Timestamp}");
        
        await Task.CompletedTask;
    }
}
```
### 3. 分布式 EventBus
#### Docker 启动 RabbitMQ 🐳

```
docker run -d --name rabbitmq \
  -e RABBITMQ_DEFAULT_USER=123 \
  -e RABBITMQ_DEFAULT_PASS=123 \
  -p 15672:15672 -p 5672:5672 \
  rabbitmq:3-management
```
#### 分布式事件定义 🌍

```
public class DistributedEventMessage : IEvent
{
    public string Message { get; set; }
    public string Source { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
}
```

#### 分布式事件处理器 🔄

```
public class MyDistributedEventHandler : IDistributedEventHandler<DistributedEventMessage>
{
    private readonly ILogger<MyDistributedEventHandler> _logger;

    public MyDistributedEventHandler(ILogger<MyDistributedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(DistributedEventMessage eventMessage)
    {
        _logger.LogInformation($"处理分布式事件: {eventMessage.Message} from {eventMessage.Source}");
        
        // 🔧 业务逻辑处理
        await ProcessBusinessLogic(eventMessage);
        
        await Task.CompletedTask;
    }
    
    private async Task ProcessBusinessLogic(DistributedEventMessage message)
    {
        // 业务处理代码
        await Task.Delay(100);
    }
}
```
#### ⚡ 高级特性
失败重试机制 🔄 发送方配置
```
services.EasyCoreEventBus(options =>
{
    options.RabbitMQ(opt =>
    {
        opt.HostName = "192.168.157.142";
        opt.UserName = "123";
        opt.Password = "123";
        opt.Port = 5672;
    });

    // 🔧 重试配置
    options.RetryCount = 3;      // 失败重试次数
    options.RetryInterval = 5;   // 重试间隔(秒)
});
```

#### 接收方配置

```
services.EasyCoreEventBus(options =>
{
    options.RabbitMQ(opt =>
    {
        opt.HostName = "192.168.157.142";
        opt.UserName = "123";
        opt.Password = "123";
        opt.Port = 5672;
    });
    
    // 🚨 失败回调函数
    options.FailureCallback = (key, message) =>
    {
        MessageBox.Show($"事件处理失败: {message}", 
            "错误", 
            MessageBoxButtons.OK, 
            MessageBoxIcon.Error);
    };
});
```
### 4.各消息队列配置示例📊 
#### 1.Kafka 配置 🔥
```
builder.Services.EasyCoreEventBus(options =>
{
    options.Kafka("localhost:9092");
});
```
#### 2.Pulsar 配置 ⚡
```
builder.Services.EasyCoreEventBus(options =>
{
    options.Pulsar("pulsar://localhost:6650");
});
```
#### 3.RabbitMQ 配置 🐇
```
builder.Services.EasyCoreEventBus(options =>
{
    options.RabbitMQ("localhost");
});
```
#### 4.Redis Streams 配置 🔴
```
builder.Services.EasyCoreEventBus(options =>
{
    options.RedisStreams(new List<string> { "localhost:6379" });
});
```
### 5.使用示例🎮 
#### 发布事件
```
[Route("api/[controller]")]
[ApiController]
public class PublishController : ControllerBase
{
    private readonly IDistributedEventBus _distributedEventBus;

    public PublishController(IDistributedEventBus distributedEventBus)
    {
        _distributedEventBus = distributedEventBus;
    }

    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] string message)
    {
        var eventMessage = new WebEventMessage()
        {
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        await _distributedEventBus.PublishAsync(eventMessage);
        
        return Ok(new { success = true, eventId = eventMessage.EventId });
    }
}
```
#### 事件处理监控 📈
```
public class MonitoringEventHandler : IDistributedEventHandler<WebEventMessage>
{
    private readonly ILogger<MonitoringEventHandler> _logger;
    private readonly IMetricsService _metrics;

    public MonitoringEventHandler(ILogger<MonitoringEventHandler> logger, IMetricsService metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public async Task HandleAsync(WebEventMessage eventMessage)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation($"开始处理事件: {eventMessage.Message}");
            
            // 📊 记录指标
            _metrics.IncrementEventCount();
            
            await ProcessEvent(eventMessage);
            
            stopwatch.Stop();
            _metrics.RecordProcessingTime(stopwatch.ElapsedMilliseconds);
            
            _logger.LogInformation($"事件处理完成: {eventMessage.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"事件处理失败: {eventMessage.Message}");
            _metrics.IncrementErrorCount();
            throw;
        }
    }
}
```
#### 🏗️ 架构优势

| 特性       | 优势    | 说明                                     |
|----------|-------|----------------------------------------|
| 🔌 多队列支持 | 灵活选择  | 支持 Kafka、Pulsar、RabbitMQ、Redis Streams |
| ⚡ 高性能    | 低延迟   | 优化的消息序列化和传输机制                          |
| 🔒 可靠性   | 消息持久化 | 支持失败重试                                 |
| 🎯 易用性   | 简单API | 统一的发布-订阅接口                             |
| 🔧 可扩展   | 插件化架构 | 易于扩展新的消息队列支持                           |


###  6.总结📝
EasyCore.EventBus 为 .NET Core 应用程序提供了一个功能丰富、易于使用的事件总线解决方案。无论是单体应用中的模块解耦，还是微服务架构中的跨服务通信，都能通过统一的 API 轻松实现。其强大的失败重试机制和多消息队列支持，让开发者可以专注于业务逻辑，而不用关心底层通信细节。

开始使用 EasyCore.EventBus，构建更加松耦合、可扩展的 .NET Core 应用程序！🎉