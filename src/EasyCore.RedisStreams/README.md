# 🔴 EasyCore.RedisStreams

> **EasyCore.RedisStreams** 是面向 .NET 8 的 Redis Streams 基础设施客户端封装。基于 [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis)，提供 DI 注册、连接、Consumer Group 订阅与确认，以及统一的消息字段格式（`RedisHeader` / `type` / `payload`），**不绑定** `IEvent` / EventBus，可单独使用，也可被 `EasyCore.EventBus.RedisStreams` 适配层复用。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![Redis](https://img.shields.io/badge/StackExchange.Redis-2.8-orange)
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
- [6. 消息格式（RedisStreamMessageFormat）](#6-消息格式redisstreammessageformat)
- [7. API 一览](#7-api-一览)
- [8. 配置项（RedisStreamsOptions）](#8-配置项redisstreamsoptions)
- [9. FAQ](#9-faq)
- [10. License](#10-license)

---

## 1. 🎯 项目定位

| 场景 | 是否适用 |
|---|---|
| 直接对 Redis Stream Key 做发布/订阅 | ✅ |
| 需要 Consumer Group + Acknowledge | ✅ |
| 统一 `type` + JSON `payload` + 重试头 | ✅ |
| 想用统一 `IEvent` + 处理器扫描（EDA） | ❌ → 请用 `EasyCore.EventBus.RedisStreams` |
| 进程内本地事件 | ❌ → 请用 `EasyCore.EventBus` 本地总线 |

### 1.1 设计原则

| 原则 | 说明 |
|---|---|
| **基础设施层** | EndPoints、Consumer Group 与 Stream 条目字段 |
| **可独立引用** | 不依赖 `EasyCore.EventBus` 核心包 |
| **格式约定清晰** | `RedisStreamMessageFormat` 固定字段名，便于跨服务互通 |
| **适配友好** | EventBus RedisStreams 适配器内部复用本包客户端 |

---

## 2. 🔗 与 EventBus 的关系

```text
┌─────────────────────────────┐
│  业务代码（JSON payload）     │
└──────────────┬──────────────┘
               │ IRedisStreamsClient
               ▼
┌─────────────────────────────┐
│ EasyCore.RedisStreams（本包） │  ← 基础设施客户端
└──────────────┬──────────────┘
               │ StackExchange.Redis
               ▼
            Redis

┌─────────────────────────────┐
│ EasyCore.EventBus.RedisStreams │  ← 适配层：IEvent / Handler
│   └── 内部使用本包客户端        │
└─────────────────────────────┘
```

| 包 | 角色 |
|---|---|
| `EasyCore.RedisStreams` | 通用 Redis Streams 客户端（本包） |
| `EasyCore.EventBus.RedisStreams` | 将 EventBus 的 `IEvent` 映射到 Redis Streams |

---

## 3. ⚙ 环境要求

| 项 | 要求 |
|---|---|
| .NET | 8.0+ |
| 依赖 | `StackExchange.Redis` 2.8.x（由本包引入） |
| Redis | 支持 Streams（Redis 5.0+）的实例 |

---

## 4. 📥 安装

```bash
dotnet add package EasyCore.RedisStreams
```

---

## 5. ⚡ 快速开始

### 5️⃣.1️⃣ 注册 DI

```csharp
using EasyCore.RedisStreams;

builder.Services.EasyCoreRedisStreams(o =>
{
    o.EndPoints = new List<string> { "localhost:6379" };
    o.User = null;           // ACL 用户名（可选）
    o.Password = null;       // 密码（可选）
    o.ConnectTimeout = 10;   // 秒
    o.SyncTimeout = 10;      // 秒
    o.DefaultDatabase = 0;
    o.ConsumerGroup = "RedisGroup";
    o.AppName = "MyApp";
});
```

### 5️⃣.2️⃣ 发布消息

```csharp
public sealed class NotifyPublisher
{
    private readonly IRedisStreamsClient _client;

    public NotifyPublisher(IRedisStreamsClient client) => _client = client;

    public async Task PublishAsync(string userId, CancellationToken ct = default)
    {
        await _client.ConnectAsync(ct);

        var payload = $$"""{"userId":"{{userId}}","channel":"sms"}""";
        var header = new RedisStreamHeader
        {
            RetryCount = 0,
            RetryInterval = 5
        };

        await _client.PublishAsync(
            streamKey: "notify:sms",
            typeName: "SmsNotify",
            payloadJson: payload,
            header: header,
            cancellationToken: ct);
    }
}
```

### 5️⃣.3️⃣ 订阅与 Acknowledge

```csharp
public sealed class NotifyConsumer : BackgroundService
{
    private readonly IRedisStreamsClient _client;

    public NotifyConsumer(IRedisStreamsClient client) => _client = client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync(stoppingToken);

        await _client.SubscribeAsync(
            streamKeys: new[] { "notify:sms" },
            handler: async (msg, ct) =>
            {
                // msg.TypeName / msg.PayloadJson / msg.Header
                // 处理业务…

                await _client.AcknowledgeAsync(msg.StreamKey, msg.MessageId, ct);
            },
            cancellationToken: stoppingToken);
    }
}
```

`RedisStreamsDeliveredMessage` 字段：`StreamKey`、`MessageId`、`TypeName`、`PayloadJson`、`Header`。

---

## 6. 📦 消息格式（RedisStreamMessageFormat）

每条 Stream 条目使用固定字段名：

| 字段常量 | Redis 字段名 | 内容 |
|---|---|---|
| `HeaderField` | `RedisHeader` | `RedisStreamHeader` 的 JSON |
| `TypeField` | `type` | 逻辑消息类型名 |
| `PayloadField` | `payload` | 业务 JSON 载荷 |

`RedisStreamHeader`：

| 属性 | 说明 |
|---|---|
| `RetryCount` | 已重试次数 |
| `RetryInterval` | 重试间隔（与 EventBus 集成约定的单位，通常为秒/毫秒语义由上层决定） |

`header` 参数为 `null` 时，发布会写入默认头。

---

## 7. 🧩 API 一览

| 成员 | 说明 |
|---|---|
| `EasyCoreRedisStreams(Action<RedisStreamsOptions>)` | DI 扩展：注册 Options 与 `IRedisStreamsClient` |
| `ConnectAsync` | 连接 Redis 并准备 Consumer Group 名称 |
| `PublishAsync(streamKey, typeName, payloadJson, header?)` | `XADD` 写入统一字段 |
| `SubscribeAsync(streamKeys, handler)` | Consumer Group 消费并启动后台循环 |
| `AcknowledgeAsync(streamKey, messageId)` | `XACK` 确认条目 |

`IRedisStreamsClient` 实现 `IAsyncDisposable`，宿主退出时应释放。

---

## 8. 🛠 配置项（RedisStreamsOptions）

| 属性 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `EndPoints` | `List<string>` | 空列表 | Redis 端点，如 `localhost:6379` |
| `User` | `string?` | `null` | ACL 用户名 |
| `Password` | `string?` | `null` | 密码 |
| `ConnectTimeout` | `int` | `10` | 连接超时（**秒**，内部 ×1000） |
| `SyncTimeout` | `int` | `10` | 同步超时（**秒**，内部 ×1000） |
| `AbortOnConnectFail` | `bool` | `false` | 首次连接失败是否中止 |
| `DefaultDatabase` | `int` | `0` | 默认库索引 |
| `ConsumerGroup` | `string` | `RedisGroup` | Group 名后缀（与 AppName 组合） |
| `AppName` | `string?` | `null` | Group 命名前缀；`null` 时用入口程序集名 |

`ToConfigurationOptions()` 可将上述项转换为 `StackExchange.Redis.ConfigurationOptions`。

---

## 9. ❓ FAQ

**Q: 本包和 `EasyCore.EventBus.RedisStreams` 选哪个？**  
A: 需要 `IEvent`、自动 Handler、重试策略时用 EventBus 适配包；只需 Stream Key + JSON 收发时直接引用本包。

**Q: 为什么要固定 `RedisHeader` / `type` / `payload`？**  
A: 便于多服务、多语言读写同一 Stream，并与 EventBus 适配层字段一致。

**Q: `ConnectTimeout` 单位是毫秒吗？**  
A: 否，Options 中为**秒**；转换到 StackExchange.Redis 时会乘以 1000。

**Q: 必须先创建 Consumer Group 吗？**  
A: 客户端在订阅流程中会按配置准备 Group；确保 Redis 版本支持 Streams，且流 Key 可被消费。

**Q: 集群 / 哨兵怎么配？**  
A: 在 `EndPoints` 填入多个端点，并配合 `User` / `Password`；高级拓扑可在连接后按 StackExchange.Redis 文档扩展。

---

## 10. 📄 License

MIT OR Apache-2.0 — 详见仓库根目录 [LICENSE](../../LICENSE)（若存在）或 NuGet 包元数据。

---

## 🤝 贡献

欢迎 Issue / PR。修改本包后请同步验证 `EasyCore.EventBus.RedisStreams` 适配层与相关 Demo。
