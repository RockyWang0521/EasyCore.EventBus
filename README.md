# EasyCore.EventBus

#### 介绍
EasyCore.EventBus
轻松实现 .Net Core EventBus

#### EventBus
EventBus（事件总线）是一个用于事件驱动架构（EDA, Event-Driven Architecture）中的基础组件。它允许不同组件、模块或服务之间进行异步通信，常用于分布式系统、微服务架构以及单体应用的模块之间。
EventBus 通过在应用程序内部提供一种发布-订阅（Pub/Sub）模式的机制来帮助实现不同部分的解耦。发布者不需要知道订阅者的具体实现，而订阅者也不需要了解发布者的信息。事件总线可以是内存中的（例如在同一进程内传递消息），也可以是基于外部消息队列（例如 RabbitMQ、Kafka、Redis）的系统。
EventBus 的核心概念是事件和订阅者。事件表示系统中发生的某种行为或状态的变化，而订阅者是处理这些事件的对象。EventBus 本身充当了事件的传输媒介。发布者发布事件后，EventBus 会根据事件的类型将其传递给所有已订阅该事件类型的订阅者。

发布（Publish）：发布者将事件推送到 EventBus，通常包含一些与事件相关的信息。
订阅（Subscribe）：订阅者订阅感兴趣的事件类型，当这些事件被发布时，订阅者会接收到事件并作出响应。

EventBus 的好处在于它促进了组件之间的松耦合。发布者无需关心事件会被谁处理，也无需了解具体的处理逻辑；同样，订阅者也不用关心事件是由哪个发布者发出的，或者事件的生成和发布机制如何。

EventBus 可以根据其实现的不同，分为几种不同的类型：

内存 EventBus：所有的事件都在内存中传播，适用于单体应用或同一进程内的组件之间的通信。
基于消息队列的 EventBus：通过消息队列（如 RabbitMQ、Kafka、Redis 等）将事件传递到其他系统或服务中。适用于微服务架构和分布式系统。
这种基于消息队列的 EventBus 能够支持跨进程、跨机器甚至跨网络的事件传递，通常具有较高的可扩展性和容错性。

此项目实现了winfrom之间的EventBus，以及winform与web之间的EventBus。基于RabbitMQ作为消息传递的媒介。


#### 使用说明

EasyCore.EventBus提供了多个消息队列支持包

```
EasyCore.EventBus
EasyCore.EventBus.Kafka
EasyCore.EventBus.Pulsar
EasyCore.EventBus.RabbitMQ
```
可根据需要下载对应的支持包，支持的队列有Kafka、Pulsar、RabbitMQ。


1.  本地EventBus

winform注册

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
web注册

```
  public class Program
  {
      public static void Main(string[] args)
      {
          var builder = WebApplication.CreateBuilder(args);

          // Add services to the container.

          builder.Services.AddControllers();
          // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
```

继承事件基础类
```
    public class LocalEventMessage : IEvent
    {
        public string Message { get; set; }
    }
```
实现抽象ILocalEventHandler中的细节

```
    public class MyLocalEventHandler : ILocalEventHandler<LocalEventMessage>
    {
        public async Task HandleAsync(LocalEventMessage eventMessage)
        {
            // Do something with the event message

            await Task.CompletedTask;
        }
    }
```


2.  分布式EventBus
docker启动一个RabbitMQ

```
docker run -d  --name rabbitmq -e RABBITMQ_DEFAULT_USER=123 -e RABBITMQ_DEFAULT_PASS=123 -p 15672:15672 -p 5672:5672 rabbitmq:3-management
```

继承事件基础类
```
    public class DistributedEventMessage : IEvent
    {
        public string Message { get; set; }
    }
```
实现抽象IDistributedEventHandler中的细节

```
  public class MyDistributedEventHandler : IDistributedEventHandler<WebDistributedEventMessage>
  {
      public async Task HandleAsync(WebDistributedEventMessage eventMessage)
      {
          // Do something with the event message

          await Task.CompletedTask;
      }
  }
```
3. 失败执行回调
注册环节中提供了 失败重试次数、失败重试时间以及失败执行回调函数(如果接收方接收到消息程序执行失败，就会间隔重试时间再次执行。执行多次达到设定的失败重试次数还是未执行成功，就会执行失败回调函数)


发送方指定 失败重试次数、失败重试时间
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

   //失败重试次数
   options.RetryCount = 3;
   //失败重试时间
   options.RetryInterval = 5;
});
```

接受方指定 失败执行回调函数
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
   //失败执行回调函数
   options.FailureCallback = (key, mes) =>
   {
       MessageBox.Show(mes, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
   };
});
```

### EasyCore.EventBus.Kafka

1.注册

```
using EasyCore.EventBus;
using EasyCore.EventBus.Kafka;

builder.Services.EasyCoreEventBus(options =>
{
   options.Kafka("localhost:9092");
});
```

2.发布和订阅

发布
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
订阅

```
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

### EasyCore.EventBus.Pulsar

1.注册

```
using EasyCore.EventBus;
using EasyCore.EventBus.Pulsar;

builder.Services.EasyCoreEventBus(options =>
{
    options.Pulsar("pulsar://localhost:6650");
});
```

2.发布和订阅

发布
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
订阅

```
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
### EasyCore.EventBus.RabbitMQ

1.注册
```
using EasyCore.EventBus;
using EasyCore.EventBus.RabbitMQ;

builder.Services.EasyCoreEventBus(options =>
{
   options.RabbitMQ("localhost");
});
```

2.发布和订阅

发布
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
订阅

```
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












