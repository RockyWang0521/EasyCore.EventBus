# 🐇 EasyCore.RabbitMQ

> **EasyCore.RabbitMQ** 是面向 .NET 8 的 RabbitMQ 基础设施客户端封装。基于 [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client)，提供 DI 注册、连接、发布、订阅与 Ack/Nack，**不绑定** `IEvent` / EventBus，可单独使用，也可被 `EasyCore.EventBus.RabbitMQ` 适配层复用。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ.Client-6.8-orange)
![License](https://img.shields.io/badge/License-MIT%20%7C%20Apache--2.0-yellow)
![Version](https://img.shields.io/badge/Version-8.0.1-blue)

---

## 🌍 Language

- **中文（当前文档）**
- English: [README.en.md](./README.en.md)

---

## 📚 目录

- [1. 项目定位](#1-项目定位)
- [2. 与 EventBus 的关系](#2-与-eventbus-的关系)
- [3. 环境要求](#3-环境要求)
- [4. 安装](#4-安装)
- [5. 快速开始](#5-快速开始)
- [6. API 一览](#6-api-一览)
- [7. 配置项（RabbitMQOptions）](#7-配置项rabbitmqoptions)
- [8. FAQ](#8-faq)
- [9. License](#9-license)

---

## 1. 🎯 项目定位

| 场景 | 是否适用 |
|---|---|
| 直接对 RabbitMQ 做字节级发布/订阅 | ✅ |
| 需要 AMQP headers、Ack/Nack 精细控制 | ✅ |
| 想用统一 `IEvent` + 处理器扫描（EDA） | ❌ → 请用 `EasyCore.EventBus.RabbitMQ` |
| 进程内本地事件 | ❌ → 请用 `EasyCore.EventBus` 本地总线 |

### 1.1 设计原则

| 原则 | 说明 |
|---|---|
| **基础设施层** | 只关心连接、拓扑与消息字节，不假设业务事件模型 |
| **可独立引用** | 不依赖 `EasyCore.EventBus` 核心包 |
| **适配友好** | EventBus RabbitMQ 适配器内部复用本包客户端 |

---

## 2. 🔗 与 EventBus 的关系

```text
┌─────────────────────────────┐
│  业务代码（任意 payload）      │
└──────────────┬──────────────┘
               │ IRabbitMQClient
               ▼
┌─────────────────────────────┐
│  EasyCore.RabbitMQ（本包）    │  ← 基础设施客户端
└──────────────┬──────────────┘
               │ RabbitMQ.Client
               ▼
          RabbitMQ Broker

┌─────────────────────────────┐
│ EasyCore.EventBus.RabbitMQ  │  ← 适配层：IEvent / Handler
│   └── 内部使用本包客户端      │
└─────────────────────────────┘
```

| 包 | 角色 |
|---|---|
| `EasyCore.RabbitMQ` | 通用 AMQP 客户端（本包） |
| `EasyCore.EventBus.RabbitMQ` | 将 EventBus 的 `IEvent` 映射到 RabbitMQ |

---

## 3. ⚙ 环境要求

| 项 | 要求 |
|---|---|
| .NET | 8.0+ |
| 依赖 | `RabbitMQ.Client` 6.8.x（由本包引入） |
| Broker | 可访问的 RabbitMQ 实例 |

---

## 4. 📥 安装

```bash
dotnet add package EasyCore.RabbitMQ
```

---

## 5. ⚡ 快速开始

### 5️⃣.1️⃣ 注册 DI

```csharp
using EasyCore.RabbitMQ;

builder.Services.EasyCoreRabbitMQ(o =>
{
    o.HostName = "localhost";
    o.UserName = "guest";
    o.Password = "guest";
    o.Port = 5672;
    o.ExchangeName = "EasyCore.EventBus";
    o.ExchangeType = "topic";
    o.VirtualHost = "/";
});
```

### 5️⃣.2️⃣ 发布消息

```csharp
public sealed class OrderPublisher
{
    private readonly IRabbitMQClient _client;

    public OrderPublisher(IRabbitMQClient client) => _client = client;

    public async Task PublishAsync(string orderId, CancellationToken ct = default)
    {
        await _client.ConnectAsync(ct);

        var body = Encoding.UTF8.GetBytes($$"""{"orderId":"{{orderId}}"}""");
        var headers = new Dictionary<string, object>
        {
            ["x-message-type"] = "OrderCreated"
        };

        await _client.PublishAsync(
            routingKey: "order.created",
            body: body,
            headers: headers,
            cancellationToken: ct);
    }
}
```

### 5️⃣.3️⃣ 订阅与 Ack / Nack

```csharp
public sealed class OrderConsumer : BackgroundService
{
    private readonly IRabbitMQClient _client;

    public OrderConsumer(IRabbitMQClient client) => _client = client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync(stoppingToken);

        await _client.SubscribeAsync(
            routingKeys: new[] { "order.created", "order.updated" },
            handler: async (msg, ct) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(msg.Body.Span);
                    // 处理业务…
                    _client.Ack(msg.DeliveryTag);
                }
                catch
                {
                    _client.Nack(msg.DeliveryTag, requeue: true);
                }

                await Task.CompletedTask;
            },
            cancellationToken: stoppingToken);
    }
}
```

`RabbitMQDeliveredMessage` 字段：`RoutingKey`、`Body`、`Headers`、`DeliveryTag`、`CorrelationId`。

---

## 6. 🧩 API 一览

| 成员 | 说明 |
|---|---|
| `EasyCoreRabbitMQ(Action<RabbitMQOptions>)` | DI 扩展：注册 Options、连接工厂、`IRabbitMQClient` |
| `ConnectAsync` | 连接 Broker 并声明配置的 Exchange |
| `PublishAsync(routingKey, body, headers?)` | 向 Exchange 发布消息（带确认） |
| `SubscribeAsync(routingKeys, handler)` | 声明队列、绑定路由键并开始消费 |
| `Ack(deliveryTag)` | 确认消息 |
| `Nack(deliveryTag, requeue)` | 拒绝消息；`requeue=true` 时重新入队 |

`IRabbitMQClient` 实现 `IAsyncDisposable`，宿主退出时应释放。

---

## 7. 🛠 配置项（RabbitMQOptions）

| 属性 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `HostName` | `string` | `localhost` | 主机名；集群可用逗号分隔 |
| `UserName` | `string` | `guest` | 用户名 |
| `Password` | `string` | `guest` | 密码 |
| `Port` | `int` | `5672` | AMQP 端口 |
| `ExchangeName` | `string` | `EasyCore.EventBus` | Exchange 名称 |
| `QueueName` | `string` | `EasyCore.Queue` | 队列名后缀（与 AppName 组合） |
| `ExchangeType` | `string` | `topic` | `topic` / `direct` / `fanout` / `headers` |
| `VirtualHost` | `string` | `/` | 虚拟主机 |
| `MessageTTL` | `int` | `864000000` | 队列消息 TTL（毫秒，默认约 10 天） |
| `QueueMode` | `string?` | `null` | 可选，如 `lazy` |
| `Durable` | `bool` | `true` | 队列是否持久化 |
| `Exclusive` | `bool` | `false` | 是否排他队列 |
| `AutoDelete` | `bool` | `false` | 是否自动删除 |
| `QueueType` | `string?` | `null` | 可选，如 `quorum` |
| `AppName` | `string?` | `null` | 队列命名前缀；`null` 时用入口程序集名 |

---

## 8. ❓ FAQ

**Q: 本包和 `EasyCore.EventBus.RabbitMQ` 选哪个？**  
A: 需要 `IEvent`、自动 Handler、重试策略时用 EventBus 适配包；只需原始字节发布/订阅时直接引用本包。

**Q: 必须先 `ConnectAsync` 吗？**  
A: 是。发布或订阅前应先连接，以完成连接与 Exchange 声明。

**Q: 订阅时队列名如何生成？**  
A: 由 `AppName`（或入口程序集名）与 `QueueName` 组合生成，再按 `routingKeys` 绑定。

**Q: `Nack` 的 `requeue` 怎么选？**  
A: 临时故障可 `requeue: true`；毒消息建议 `false`（或配合死信队列），避免无限循环。

**Q: 能否多机集群 Host？**  
A: `HostName` 支持逗号分隔多个主机，具体故障转移行为遵循 RabbitMQ.Client。

---

## 9. 📄 License

MIT OR Apache-2.0 — 详见仓库根目录 [LICENSE](../../LICENSE)（若存在）或 NuGet 包元数据。

---

## 🤝 贡献

欢迎 Issue / PR。修改本包后请同步验证 `EasyCore.EventBus.RabbitMQ` 适配层与相关 Demo。
