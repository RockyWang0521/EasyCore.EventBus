# 🔴 EasyCore.RedisStreams

> **EasyCore.RedisStreams** is a Redis Streams infrastructure client for .NET 8. Built on [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis), it provides DI registration, connect, consumer-group subscribe/ack, and a unified entry format (`RedisHeader` / `type` / `payload`). It is **not tied** to `IEvent` / EventBus — use it standalone, or via the `EasyCore.EventBus.RedisStreams` adapter.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![Redis](https://img.shields.io/badge/StackExchange.Redis-2.8-orange)
![License](https://img.shields.io/badge/License-MIT%20%7C%20Apache--2.0-yellow)
![Version](https://img.shields.io/badge/Version-8.3.0-blue)

---

## 🌍 Language

- Chinese: [README.md](./README.md)
- **English (this document)**

---

## 📚 Table of Contents

- [1. Positioning](#1-positioning)
- [2. Relation to EventBus](#2-relation-to-eventbus)
- [3. Requirements](#3-requirements)
- [4. Installation](#4-installation)
- [5. Quick Start](#5-quick-start)
- [6. Message Format (`RedisStreamMessageFormat`)](#6-message-format-redisstreammessageformat)
- [7. API Overview](#7-api-overview)
- [8. Options (`RedisStreamsOptions`)](#8-options-redisstreamsoptions)
- [9. FAQ](#9-faq)
- [10. License](#10-license)

---

## 1. 🎯 Positioning

| Scenario | Fit? |
|---|---|
| Publish/subscribe on Redis stream keys | ✅ |
| Consumer groups + acknowledge | ✅ |
| Unified `type` + JSON `payload` + retry header | ✅ |
| Unified `IEvent` + handler discovery (EDA) | ❌ → use `EasyCore.EventBus.RedisStreams` |
| In-process local events | ❌ → use `EasyCore.EventBus` local bus |

### 1.1 Design Principles

| Principle | Meaning |
|---|---|
| **Infrastructure layer** | EndPoints, consumer groups, and stream entry fields |
| **Standalone** | No dependency on the `EasyCore.EventBus` core package |
| **Clear wire format** | Fixed field names in `RedisStreamMessageFormat` |
| **Adapter-friendly** | The EventBus RedisStreams adapter reuses this client |

---

## 2. Relation to EventBus

```text
┌─────────────────────────────┐
│  App code (JSON payload)    │
└──────────────┬──────────────┘
               │ IRedisStreamsClient
               ▼
┌─────────────────────────────┐
│ EasyCore.RedisStreams (this)│  ← infrastructure client
└──────────────┬──────────────┘
               │ StackExchange.Redis
               ▼
            Redis

┌─────────────────────────────┐
│ EasyCore.EventBus.RedisStreams │  ← adapter: IEvent / Handler
│   └── uses this client         │
└─────────────────────────────┘
```

| Package | Role |
|---|---|
| `EasyCore.RedisStreams` | Generic Redis Streams client (this package) |
| `EasyCore.EventBus.RedisStreams` | Maps EventBus `IEvent` onto Redis Streams |

---

## 3. ⚙ Requirements

| Item | Requirement |
|---|---|
| .NET | 8.0+ |
| Dependency | `StackExchange.Redis` 2.8.x (brought by this package) |
| Redis | Instance with Streams support (Redis 5.0+) |

---

## 4. 📥 Installation

```bash
dotnet add package EasyCore.RedisStreams
```

---

## 5. ⚡ Quick Start

### 5️⃣.1️⃣ Register DI

```csharp
using EasyCore.RedisStreams;

builder.Services.AddEasyCoreRedisStreams(o =>
{
    o.EndPoints = new List<string> { "localhost:6379" };
    o.User = null;           // ACL user (optional)
    o.Password = null;       // password (optional)
    o.ConnectTimeout = 10;   // seconds
    o.SyncTimeout = 10;      // seconds
    o.DefaultDatabase = 0;
    o.ConsumerGroup = "RedisGroup";
    o.AppName = "MyApp";
});
```

### 5️⃣.2️⃣ Publish

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

### 5️⃣.3️⃣ Subscribe and Acknowledge

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
                // handle business…

                await _client.AcknowledgeAsync(msg.StreamKey, msg.MessageId, ct);
            },
            cancellationToken: stoppingToken);
    }
}
```

`RedisStreamsDeliveredMessage` fields: `StreamKey`, `MessageId`, `TypeName`, `PayloadJson`, `Header`.

---

## 6. Message Format (`RedisStreamMessageFormat`)

Each stream entry uses fixed field names:

| Constant | Redis field | Content |
|---|---|---|
| `HeaderField` | `RedisHeader` | JSON of `RedisStreamHeader` |
| `TypeField` | `type` | Logical message type name |
| `PayloadField` | `payload` | Business JSON payload |

`RedisStreamHeader`:

| Property | Description |
|---|---|
| `RetryCount` | Times processing has been retried |
| `RetryInterval` | Retry interval (unit agreed by the EventBus integration layer) |

When `header` is `null`, publish writes a default header.

---

## 7. 🧩 API Overview

| Member | Description |
|---|---|
| `AddEasyCoreRedisStreams(Action<RedisStreamsOptions>)` | DI: Options and `IRedisStreamsClient` |
| `ConnectAsync` | Connect to Redis and prepare the consumer group name |
| `PublishAsync(streamKey, typeName, payloadJson, header?)` | `XADD` with unified fields |
| `SubscribeAsync(streamKeys, handler)` | Consumer-group consume loop |
| `AcknowledgeAsync(streamKey, messageId)` | `XACK` the entry |

`IRedisStreamsClient` implements `IAsyncDisposable` — dispose on shutdown.

---

## 8. Options (`RedisStreamsOptions`)

| Property | Type | Default | Description |
|---|---|---|---|
| `EndPoints` | `List<string>` | empty | Redis endpoints, e.g. `localhost:6379` |
| `User` | `string?` | `null` | ACL username |
| `Password` | `string?` | `null` | Password |
| `ConnectTimeout` | `int` | `10` | Connect timeout (**seconds**, ×1000 internally) |
| `SyncTimeout` | `int` | `10` | Sync timeout (**seconds**, ×1000 internally) |
| `AbortOnConnectFail` | `bool` | `false` | Abort when initial connect fails |
| `DefaultDatabase` | `int` | `0` | Default DB index |
| `ConsumerGroup` | `string` | `RedisGroup` | Group name suffix (combined with AppName) |
| `AppName` | `string?` | `null` | Group name prefix; entry assembly when null |

`ToConfigurationOptions()` converts these settings to `StackExchange.Redis.ConfigurationOptions`.

---

## 9. ❓ FAQ

**Q: This package vs `EasyCore.EventBus.RedisStreams`?**  
A: Use the EventBus adapter for `IEvent`, handlers, and retries. Use this package for stream-key + JSON I/O.

**Q: Why fixed `RedisHeader` / `type` / `payload`?**  
A: So multiple services (and the EventBus adapter) can share the same stream layout.

**Q: Is `ConnectTimeout` in milliseconds?**  
A: No — Options use **seconds**; values are multiplied by 1000 for StackExchange.Redis.

**Q: Do I need to create the consumer group myself?**  
A: The client prepares the group during subscribe. Ensure Redis supports Streams and the key is consumable.

**Q: Cluster / sentinel?**  
A: Put endpoints in `EndPoints` with credentials as needed; advanced topologies follow StackExchange.Redis docs.

---

## 10. 📄 License

MIT — see the repository [LICENSE](../../LICENSE), or the NuGet package metadata.

---

## 🤝 Contributing

Issues and PRs are welcome. After changing this package, verify `EasyCore.EventBus.RedisStreams` and related demos.
