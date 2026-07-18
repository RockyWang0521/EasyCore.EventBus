# ⚡ EasyCore.Pulsar

> **EasyCore.Pulsar** 是面向 .NET 8 的 Apache Pulsar 基础设施客户端封装。基于 [Pulsar.Client](https://www.nuget.org/packages/Pulsar.Client)，提供 DI 注册、连接、按 Topic 发布/订阅与 `AcknowledgeAsync`，**不绑定** `IEvent` / EventBus，可单独使用，也可被 `EasyCore.EventBus.Pulsar` 适配层复用。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![Pulsar](https://img.shields.io/badge/Pulsar.Client-3.12-orange)
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
- [7. 配置项（PulsarOptions）](#7-配置项pulsaroptions)
- [8. FAQ](#8-faq)
- [9. License](#9-license)

---

## 1. 🎯 项目定位

| 场景 | 是否适用 |
|---|---|
| 直接对 Pulsar Topic 做字节级发布/订阅 | ✅ |
| 需要 Properties、按 MessageId 确认 | ✅ |
| 需要 TLS / 认证等客户端高级选项 | ✅ |
| 想用统一 `IEvent` + 处理器扫描（EDA） | ❌ → 请用 `EasyCore.EventBus.Pulsar` |
| 进程内本地事件 | ❌ → 请用 `EasyCore.EventBus` 本地总线 |

### 1.1 设计原则

| 原则 | 说明 |
|---|---|
| **基础设施层** | ServiceUrl、Topic 前缀、TLS 与消息字节 |
| **可独立引用** | 不依赖 `EasyCore.EventBus` 核心包 |
| **适配友好** | EventBus Pulsar 适配器内部复用本包客户端 |

---

## 2. 🔗 与 EventBus 的关系

```text
┌─────────────────────────────┐
│  业务代码（任意 payload）      │
└──────────────┬──────────────┘
               │ IPulsarClient
               ▼
┌─────────────────────────────┐
│  EasyCore.Pulsar（本包）      │  ← 基础设施客户端
└──────────────┬──────────────┘
               │ Pulsar.Client
               ▼
          Pulsar Cluster

┌─────────────────────────────┐
│ EasyCore.EventBus.Pulsar    │  ← 适配层：IEvent / Handler
│   └── 内部使用本包客户端      │
└─────────────────────────────┘
```

| 包 | 角色 |
|---|---|
| `EasyCore.Pulsar` | 通用 Pulsar 客户端（本包） |
| `EasyCore.EventBus.Pulsar` | 将 EventBus 的 `IEvent` 映射到 Pulsar |

---

## 3. ⚙ 环境要求

| 项 | 要求 |
|---|---|
| .NET | 8.0+ |
| 依赖 | `Pulsar.Client` 3.12.x（由本包引入） |
| Broker | 可访问的 Pulsar Service URL（如 `pulsar://localhost:6650`） |

---

## 4. 📥 安装

```bash
dotnet add package EasyCore.Pulsar
```

---

## 5. ⚡ 快速开始

### 5️⃣.1️⃣ 注册 DI

```csharp
using EasyCore.Pulsar;

builder.Services.AddEasyCorePulsar(o =>
{
    o.ServiceUrl = "pulsar://localhost:6650";
    o.TopicPrefix = "persistent://public/default/";
    o.AppName = "MyApp";
    // TLS / 认证按需配置：
    // o.UseTls = true;
    // o.TlsAllowInsecureConnection = false;
});
```

### 5️⃣.2️⃣ 发布消息

```csharp
public sealed class InvoicePublisher
{
    private readonly IPulsarClient _client;

    public InvoicePublisher(IPulsarClient client) => _client = client;

    public async Task PublishAsync(string invoiceId, CancellationToken ct = default)
    {
        await _client.ConnectAsync(ct);

        var body = Encoding.UTF8.GetBytes($$"""{"invoiceId":"{{invoiceId}}"}""");
        var properties = new Dictionary<string, string>
        {
            ["EventType"] = "InvoiceCreated"
        };

        // 相对 Topic 名会自动加上 TopicPrefix
        await _client.PublishAsync(
            topic: "invoice.created",
            body: body,
            properties: properties,
            cancellationToken: ct);
    }
}
```

### 5️⃣.3️⃣ 订阅与 Acknowledge

```csharp
public sealed class InvoiceConsumer : BackgroundService
{
    private readonly IPulsarClient _client;

    public InvoiceConsumer(IPulsarClient client) => _client = client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync(stoppingToken);

        await _client.SubscribeAsync(
            topics: new[] { "invoice.created" },
            handler: async (msg, ct) =>
            {
                var json = Encoding.UTF8.GetString(msg.Body.Span);
                // 处理业务…

                await _client.AcknowledgeAsync(msg.MessageId, ct);
            },
            cancellationToken: stoppingToken);
    }
}
```

`PulsarDeliveredMessage` 字段：`Topic`、`Body`、`Properties`、`MessageId`。

---

## 6. 🧩 API 一览

| 成员 | 说明 |
|---|---|
| `AddEasyCorePulsar(Action<PulsarOptions>)` | DI 扩展：注册 Options 与 `IPulsarClient` |
| `ConnectAsync` | 连接 ServiceUrl 并构建底层客户端 |
| `PublishAsync(topic, body, properties?)` | 发布消息；相对 Topic 会加 `TopicPrefix` |
| `SubscribeAsync(topics, handler)` | 订阅并启动后台消费循环 |
| `AcknowledgeAsync(messageId)` | 按 Pulsar `MessageId` 确认 |

`IPulsarClient` 实现 `IAsyncDisposable`，宿主退出时应释放。

---

## 7. 🛠 配置项（PulsarOptions）

| 属性 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `ServiceUrl` | `string` | `pulsar://localhost:6650` | Pulsar 服务地址 |
| `EnableClientLog` | `bool` | `false` | 是否开启底层客户端日志 |
| `UseTls` | `bool` | 库默认 | 是否启用 TLS |
| `TlsHostnameVerificationEnable` | `bool` | 库默认 | TLS 主机名校验 |
| `TlsAllowInsecureConnection` | `bool` | 库默认 | 是否允许不安全 TLS |
| `TlsTrustCertificate` | `X509Certificate2` | 库默认 | 信任证书 |
| `Authentication` | `Authentication` | 库默认 | Pulsar 认证插件/配置 |
| `TlsProtocols` | `SslProtocols` | 库默认 | 允许的 TLS 协议 |
| `TopicPrefix` | `string` | `persistent://public/default/` | 相对 Topic 名前缀 |
| `AppName` | `string?` | `null` | 订阅/消费者命名；`null` 时用入口程序集名 |

> TLS / Authentication 默认值来自 `PulsarClientConfiguration.Default`。

---

## 8. ❓ FAQ

**Q: 本包和 `EasyCore.EventBus.Pulsar` 选哪个？**  
A: 需要 `IEvent`、自动 Handler、重试策略时用 EventBus 适配包；只需 Topic 级字节收发时直接引用本包。

**Q: Topic 要写全名吗？**  
A: 可写相对名（会加 `TopicPrefix`），也可写完整 `persistent://tenant/ns/topic`。

**Q: 必须先 `ConnectAsync` 吗？**  
A: 是。发布或订阅前应先连接以构建底层 Pulsar 客户端。

**Q: 如何启用 TLS？**  
A: 设置 `UseTls = true`，并按环境配置证书、`TlsHostnameVerificationEnable`、`Authentication` 等。

---

## 9. 📄 License

MIT — 详见仓库根目录 [LICENSE](../../LICENSE) 或 NuGet 包元数据。

---

## 🤝 贡献

欢迎 Issue / PR。修改本包后请同步验证 `EasyCore.EventBus.Pulsar` 适配层与相关 Demo。
