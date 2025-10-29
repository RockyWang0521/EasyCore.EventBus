# EasyCore.EventBus

## 介绍

EasyCore.EventBus 是一个为 .NET Core 应用程序设计的事件总线框架，它简化了事件驱动架构（EDA）的实现。该框架支持多种消息队列技术，包括 RabbitMQ、Kafka 和 Pulsar，可以用于构建单体应用、分布式系统和微服务架构。

## EventBus

EventBus（事件总线）是一种基础组件，用于在事件驱动架构中实现组件之间的异步通信。它允许发布-订阅模式，使得发布者和订阅者之间解耦。

### 核心概念

- **发布（Publish）**：发布者将事件推送到 EventBus。
- **订阅（Subscribe）**：订阅者订阅感兴趣的事件类型。

### 类型

- **内存 EventBus**：适用于单体应用或同一进程内的组件之间的通信。
- **基于消息队列的 EventBus**：适用于微服务架构和分布式系统，支持跨进程、跨机器的事件传递。

## 使用说明

### 支持的消息队列

EasyCore.EventBus 提供了多个消息队列支持包：

- `EasyCore.EventBus.Kafka`
- `EasyCore.EventBus.Pulsar`
- `EasyCore.EventBus.RabbitMQ`

### 本地 EventBus

#### WinForm 注册

```csharp
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

#### Web 注册

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

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

        // Configure the HTTP request pipeline.
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

### 继承事件基础类

```csharp
public class LocalEventMessage : IEvent
{
    public string Message { get; set; }
}
```

### 实现抽象 `ILocalEventHandler` 中的细节

```csharp
public class MyLocalEventHandler : ILocalEventHandler<LocalEventMessage>
{
    public async Task HandleAsync(LocalEventMessage eventMessage)
    {
        // Do something with the event message

        await Task.CompletedTask;
    }
}
```

### 分布式 EventBus

#### Docker 启动一个 RabbitMQ

```bash
docker run -d  --name rabbitmq -e RABBITMQ_DEFAULT_USER=123 -e RABBITMQ_DEFAULT_PASS=123 -p 15672:15672 -p 5672:5672 rabbitmq:3-management
```

#### 继承事件基础类

```csharp
public class DistributedEventMessage : IEvent
{
    public string Message { get; set; }
}
```

#### 实现抽象 `IDistributedEventHandler` 中的细节

```csharp
public class MyDistributedEventHandler : IDistributedEventHandler<WebDistributedEventMessage>
{
    public async Task HandleAsync(WebDistributedEventMessage eventMessage)
    {
        // Do something with the event message

        await Task.CompletedTask;
    }
}
```

### 失败执行回调

#### 发送方指定失败重试次数和失败重试时间

```csharp
services.EasyCoreEventBus(options =>
{
   options.RabbitMQ(opt =>
   {
       opt.HostName = "192.168.157.142";
       opt.UserName = "123";
       opt.Password = "123";
       opt.Port = 5672;
   });

   // 失败重试次数
   options.RetryCount = 3;
   // 失败重试时间
   options.RetryInterval = 5;
});
```

#### 接收方指定失败执行回调函数

```csharp
services.EasyCoreEventBus(options =>
{
   options.RabbitMQ(opt =>
   {
      opt.HostName = "192.168.157.142";
      opt.UserName = "123";
      opt.Password = "123";
      opt.Port = 5672;
   });
   // 失败执行回调函数
   options.FailureCallback = (key, mes) =>
   {
       MessageBox.Show(mes, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
   };
});
```

## EasyCore.EventBus.Kafka

### 注册

```csharp
using EasyCore.EventBus;
using EasyCore.EventBus.Kafka;

builder.Services.EasyCoreEventBus(options =>
{
   options.Kafka("localhost:9092");
});
```

### 发布和订阅

#### 发布

```csharp
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
    public async Task Publish()
    {
        var em = new WebEventMessage()
        {
            Message = "Hello, world!"
        };

        await _distributedEventBus.PublishAsync(em);
    }
}
```

#### 订阅

```csharp
using EasyCore.EventBus.Event;

public class MyEventMessage : IDistributedEventHandler<WebEventMessage>
{
    private readonly ILogger<MyEventMessage> _logger;

    public MyEventMessage(ILogger<MyEventMessage> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(WebEventMessage eventMessage)
    {
        _logger.LogInformation($"Received event message: {eventMessage.Message}--{Guid.NewGuid()}");

        await Task.CompletedTask;
    }
}
```

## EasyCore.EventBus.Pulsar

### 注册

```csharp
using EasyCore.EventBus;
using EasyCore.EventBus.Pulsar;

builder.Services.EasyCoreEventBus(options =>
{
    options.Pulsar("pulsar://localhost:6650");
});
```

### 发布和订阅

#### 发布

```csharp
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
    public async Task Publish()
    {
        var em = new WebEventMessage()
        {
            Message = "Hello, world!"
        };

        await _distributedEventBus.PublishAsync(em);
    }
}
```

#### 订阅

```csharp
using EasyCore.EventBus.Event;

public class MyEventMessage : IDistributedEventHandler<WebEventMessage>
{
    private readonly ILogger<MyEventMessage> _logger;

    public MyEventMessage(ILogger<MyEventMessage> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(WebEventMessage eventMessage)
    {
        _logger.LogInformation($"Received event message: {eventMessage.Message}--{Guid.NewGuid()}");

        await Task.CompletedTask;
    }
}
```

## EasyCore.EventBus.RabbitMQ

### 注册

```csharp
using EasyCore.EventBus;
using EasyCore.EventBus.RabbitMQ;

builder.Services.EasyCoreEventBus(options =>
{
   options.RabbitMQ("localhost");
});
```

### 发布和订阅

#### 发布

```csharp
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
    public async Task Publish()
    {
        var em = new WebEventMessage()
        {
            Message = "Hello, world!"
        };

        await _distributedEventBus.PublishAsync(em);
    }
}
```

#### 订阅

```csharp
using EasyCore.EventBus.Event;

public class MyEventMessage : IDistributedEventHandler<WebEventMessage>
{
    private readonly ILogger<MyEventMessage> _logger;

    public MyEventMessage(ILogger<MyEventMessage> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(WebEventMessage eventMessage)
    {
        _logger.LogInformation($"Received event message: {eventMessage.Message}--{Guid.NewGuid()}");

        await Task.CompletedTask;
    }
}
```