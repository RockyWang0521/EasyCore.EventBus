# EasyCore.EventBus - .NET Core Event Bus Solution 🚀

[中文 README](github.com/RockyWang0521/EasyCore.EventBus/blob/master//README.md)

## 📋 Project Introduction

EasyCore.EventBus is a lightweight event bus library designed specifically for .NET Core, helping developers easily implement Event-Driven Architecture (EDA). This library supports multiple message queues as event transmission media and provides a unified event publish-subscribe interface, making asynchronous communication between different components, modules, or services simpler.

🎯 Core Concepts
Event Bus
The Event Bus is a core component in Event-Driven Architecture. Based on the Publish-Subscribe (Pub/Sub) model, it decouples different parts of the system:

| Component     | Role    | Responsibility              |
|--------|-------|-----------------|
| 📤 Publisher | Event Producer | Pushes events to the EventBus |
| 📥 Subscriber| Event Consumer | Subscribes and processes events     |
| 📨 Event  | Message Carrier  | Represents changes or actions in the system   |

🔌 Supported Message Queues
EasyCore.EventBus provides support for multiple message queues:

| Package Name                   | Message Queue       | Features      |
|--------------------------------|---------------|-------------|
| EasyCore.EventBus.Kafka        | Apache Kafka  | High throughput, distributed    |
| EasyCore.EventBus.Pulsar       | Apache Pulsar | Low latency, cloud-native   |
| EasyCore.EventBus.RabbitMQ     | RabbitMQ      | High concurrency, AMQP protocol |
| EasyCore.EventBus.RedisStreams | Redis Streams | In-memory performance, simple to use |

## 🚀 Quick Start
### 1. Local EventBus (In-Process Communication)
#### WinForms Application Configuration 🖥️
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
            
            // 🎯 Register EventBus Service
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
#### Web API Configuration 🌐
```
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // 🎯 Register EventBus Service
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
### 2. Define Events and Handlers
#### Event Definition 📨
```
public class LocalEventMessage : IEvent
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
```
#### Event Handler ⚙️
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
        // ✅ Handle event logic
        _logger.LogInformation($"Received event: {eventMessage.Message} at {eventMessage.Timestamp}");
        
        await Task.CompletedTask;
    }
}
```
### 3. Distributed EventBus
#### Docker Start RabbitMQ 🐳
```
docker run -d --name rabbitmq \
  -e RABBITMQ_DEFAULT_USER=123 \
  -e RABBITMQ_DEFAULT_PASS=123 \
  -p 15672:15672 -p 5672:5672 \
  rabbitmq:3-management
```
#### Distributed Event Definition 🌍
```
public class DistributedEventMessage : IEvent
{
    public string Message { get; set; }
    public string Source { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
}
```
#### Distributed Event Handler 🔄
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
        _logger.LogInformation($"Processing distributed event: {eventMessage.Message} from {eventMessage.Source}");
        
        // 🔧 Process business logic
        await ProcessBusinessLogic(eventMessage);
        
        await Task.CompletedTask;
    }
    
    private async Task ProcessBusinessLogic(DistributedEventMessage message)
    {
        // Business logic code
        await Task.Delay(100);
    }
}
```
#### ⚡ Advanced Features

Retry Mechanism 🔄 Sender Configuration
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

    // 🔧 Retry Configuration
    options.RetryCount = 3;      // Retry count
    options.RetryInterval = 5;   // Retry interval (seconds)
});
```
#### Receiver Configuration
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
    
    // 🚨 Failure Callback Function
    options.FailureCallback = (key, message) =>
    {
        MessageBox.Show($"Event handling failed: {message}", 
            "Error", 
            MessageBoxButtons.OK, 
            MessageBoxIcon.Error);
    };
});
```
### 4. Message Queue Configuration Examples📊
#### 1. Kafka Configuration 🔥
```
builder.Services.EasyCoreEventBus(options =>
{
    options.Kafka("localhost:9092");
});
```
#### 2. Pulsar Configuration ⚡
```
builder.Services.EasyCoreEventBus(options =>
{
    options.Pulsar("pulsar://localhost:6650");
});
```
#### 3. RabbitMQ Configuration 🐇
```
builder.Services.EasyCoreEventBus(options =>
{
    options.RabbitMQ("localhost");
});
```
#### 4. Redis Streams Configuration 🔴
```
builder.Services.EasyCoreEventBus(options =>
{
    options.RedisStreams(new List<string> { "localhost:6379" });
});
```
### 5. Usage Example 🎮
#### Publish Event
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
#### Event Handling Monitoring 📈
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
            _logger.LogInformation($"Starting event processing: {eventMessage.Message}");
            
            // 📊 Log metrics
            _metrics.IncrementEventCount();
            
            await ProcessEvent(eventMessage);
            
            stopwatch.Stop();
            _metrics.RecordProcessingTime(stopwatch.ElapsedMilliseconds);
            
            _logger.LogInformation($"Event processed: {eventMessage.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Event processing failed: {eventMessage.Message}");
            _metrics.IncrementErrorCount();
            throw;
        }
    }
}
```
#### 🏗️ Architecture Benefits

| Feature| Benefit	| Description|
|----------|-------|----------------------------------------|
| 🔌 Multi-Queue Support |Flexibility	| Supports Kafka, Pulsar, RabbitMQ, Redis Streams |
| ⚡ High Performance  | Low Latency   |Optimized message serialization and transport                    |
| 🔒 Reliability | Message Persistence | Supports retry on failure                     |
| 🎯 Easy-to-Use |  Simple API  | Unified publish-subscribe interface        |
|🔧 Scalable | Modular Architecture | Easy to extend with new message queue support                    |

### 6. Conclusion 📝

EasyCore.EventBus provides a feature-rich and easy-to-use event bus solution for .NET Core applications. Whether it's decoupling modules within a monolithic application or enabling cross-service communication in a microservices architecture, it can be easily achieved with a unified API. Its robust retry mechanism and support for multiple message queues allow developers to focus on business logic rather than worrying about underlying communication details.

Start using EasyCore.EventBus to build more loosely coupled, scalable .NET Core applications! 🎉
