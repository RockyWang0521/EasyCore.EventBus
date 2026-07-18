# 📨 EasyCore.Kafka

> **EasyCore.Kafka** 是面向 .NET 8 的 Kafka 基础设施客户端封装。基于 [Confluent.Kafka](https://www.nuget.org/packages/Confluent.Kafka)，提供 DI 注册、连接、按 Topic 发布/订阅与手动 Commit，**不绑定** `IEvent` / EventBus，可单独使用，也可被 `EasyCore.EventBus.Kafka` 适配层复用。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![Kafka](https://img.shields.io/badge/Confluent.Kafka-2.12-orange)
![License](https://img.shields.io/badge/License-MIT%20%7C%20Apache--2.0-yellow)
![Version](https://img.shields.io/badge/Version-8.3.0-blue)

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
- [7. 配置项（KafkaOptions）](#7-配置项kafkaoptions)
- [8. FAQ](#8-faq)
- [9. License](#9-license)

---

## 1. 🎯 项目定位

| 场景 | 是否适用 |
|---|---|
| 直接对 Kafka Topic 做字节级发布/订阅 | ✅ |
| 需要 Key、Headers、手动 Commit | ✅ |
| 想用统一 `IEvent` + 处理器扫描（EDA） | ❌ → 请用 `EasyCore.EventBus.Kafka` |
| 进程内本地事件 | ❌ → 请用 `EasyCore.EventBus` 本地总线 |

### 1.1 设计原则

| 原则 | 说明 |
|---|---|
| **基础设施层** | 只关心 Bootstrap、Topic、Consumer Group 与消息字节 |
| **可独立引用** | 不依赖 `EasyCore.EventBus` 核心包 |
| **适配友好** | EventBus Kafka 适配器内部复用本包客户端 |

---

## 2. 🔗 与 EventBus 的关系

```text
┌─────────────────────────────┐
│  业务代码（任意 payload）      │
└──────────────┬──────────────┘
               │ IKafkaClient
               ▼
┌─────────────────────────────┐
│  EasyCore.Kafka（本包）       │  ← 基础设施客户端
└──────────────┬──────────────┘
               │ Confluent.Kafka
               ▼
          Kafka Cluster

┌─────────────────────────────┐
│ EasyCore.EventBus.Kafka     │  ← 适配层：IEvent / Handler
│   └── 内部使用本包客户端      │
└─────────────────────────────┘
```

| 包 | 角色 |
|---|---|
| `EasyCore.Kafka` | 通用 Kafka 客户端（本包） |
| `EasyCore.EventBus.Kafka` | 将 EventBus 的 `IEvent` 映射到 Kafka |

---

## 3. ⚙ 环境要求

| 项 | 要求 |
|---|---|
| .NET | 8.0+ |
| 依赖 | `Confluent.Kafka` 2.12.x（由本包引入） |
| Broker | 可访问的 Kafka Bootstrap Servers |

---

## 4. 📥 安装

```bash
dotnet add package EasyCore.Kafka
```

---

## 5. ⚡ 快速开始

### 5️⃣.1️⃣ 注册 DI

```csharp
using EasyCore.Kafka;

builder.Services.AddEasyCoreKafka(o =>
{
    o.BootstrapServers = "localhost:9092";
    o.TopicName = "EasyCore.Topic";
    o.GroupId = "EasyCore.GroupId";
    o.MessageTimeoutMs = 10000;
    o.RequestTimeoutMs = 10000;
});
```

### 5️⃣.2️⃣ 发布消息

```csharp
public sealed class TelemetryPublisher
{
    private readonly IKafkaClient _client;

    public TelemetryPublisher(IKafkaClient client) => _client = client;

    public async Task PublishAsync(string deviceId, CancellationToken ct = default)
    {
        await _client.ConnectAsync(ct);

        var body = Encoding.UTF8.GetBytes($$"""{"deviceId":"{{deviceId}}"}""");
        var headers = new Dictionary<string, byte[]>
        {
            ["x-source"] = Encoding.UTF8.GetBytes("iot-gateway")
        };

        await _client.PublishAsync(
            topic: "telemetry.raw",
            body: body,
            key: deviceId,
            headers: headers,
            cancellationToken: ct);
    }
}
```

### 5️⃣.3️⃣ 订阅与 Commit

```csharp
public sealed class TelemetryConsumer : BackgroundService
{
    private readonly IKafkaClient _client;

    public TelemetryConsumer(IKafkaClient client) => _client = client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync(stoppingToken);

        await _client.SubscribeAsync(
            topics: new[] { "telemetry.raw" },
            handler: async (msg, ct) =>
            {
                var json = Encoding.UTF8.GetString(msg.Body.Span);
                // 处理业务…

                if (msg.NativeResult is not null)
                    _client.Commit(msg.NativeResult);

                await Task.CompletedTask;
            },
            cancellationToken: stoppingToken);
    }
}
```

`KafkaDeliveredMessage` 字段：`Topic`、`Key`、`Body`、`Headers`、`NativeResult`（传给 `Commit`）。

---

## 6. 🧩 API 一览

| 成员 | 说明 |
|---|---|
| `AddEasyCoreKafka(Action<KafkaOptions>)` | DI 扩展：注册 Options 与 `IKafkaClient` |
| `ConnectAsync` | 初始化 Consumer（幂等） |
| `PublishAsync(topic, body, key?, headers?)` | 向指定 Topic 生产消息 |
| `SubscribeAsync(topics, handler)` | 订阅多个 Topic 并启动后台消费循环 |
| `Commit(nativeResult)` | 提交与投递消息关联的 offset |

`IKafkaClient` 实现 `IAsyncDisposable`，宿主退出时应释放。

---

## 7. 🛠 配置项（KafkaOptions）

| 属性 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `BootstrapServers` | `string` | `localhost:9092` | Bootstrap 地址，逗号分隔 |
| `TopicName` | `string` | `EasyCore.Topic` | Topic 名称前缀（适配层/约定用） |
| `GroupId` | `string` | `EasyCore.GroupId` | Consumer Group 后缀 |
| `MessageTimeoutMs` | `int` | `10000` | 发送超时（毫秒） |
| `RequestTimeoutMs` | `int` | `10000` | 请求超时（毫秒） |
| `QueueBufferingMaxMessages` | `int` | `30000` | Producer 队列缓冲上限 |
| `AppName` | `string?` | `null` | 用于 GroupId 命名；`null` 时用入口程序集名 |

---

## 8. ❓ FAQ

**Q: 本包和 `EasyCore.EventBus.Kafka` 选哪个？**  
A: 需要 `IEvent`、自动 Handler、重试策略时用 EventBus 适配包；只需 Topic 级字节收发时直接引用本包。

**Q: `Commit` 要传什么？**  
A: 传入 handler 收到的 `KafkaDeliveredMessage.NativeResult`（底层 ConsumeResult）。

**Q: `TopicName` / `GroupId` 在独立使用时有何作用？**  
A: 作为命名约定与默认后缀；`PublishAsync` / `SubscribeAsync` 仍以你传入的 topic 列表为准。

**Q: 支持多 Bootstrap Server 吗？**  
A: 支持，在 `BootstrapServers` 中用逗号分隔，例如 `kafka1:9092,kafka2:9092`。

---

## 9. 📄 License

MIT — 详见仓库根目录 [LICENSE](../../LICENSE) 或 NuGet 包元数据。

---

## 🤝 贡献

欢迎 Issue / PR。修改本包后请同步验证 `EasyCore.EventBus.Kafka` 适配层与相关 Demo。
