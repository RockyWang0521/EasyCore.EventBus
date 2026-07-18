# 📨 EasyCore.Kafka

> **EasyCore.Kafka** is a Kafka infrastructure client for .NET 8. Built on [Confluent.Kafka](https://www.nuget.org/packages/Confluent.Kafka), it provides DI registration, connect, topic publish/subscribe, and manual Commit. It is **not tied** to `IEvent` / EventBus — use it standalone, or via the `EasyCore.EventBus.Kafka` adapter.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![Kafka](https://img.shields.io/badge/Confluent.Kafka-2.12-orange)
![License](https://img.shields.io/badge/License-MIT%20%7C%20Apache--2.0-yellow)
![Version](https://img.shields.io/badge/Version-8.0.1-blue)

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
- [6. API Overview](#6-api-overview)
- [7. Options (`KafkaOptions`)](#7-options-kafkaoptions)
- [8. FAQ](#8-faq)
- [9. License](#9-license)

---

## 1. 🎯 Positioning

| Scenario | Fit? |
|---|---|
| Byte-level publish/subscribe on Kafka topics | ✅ |
| Key, headers, and manual Commit | ✅ |
| Unified `IEvent` + handler discovery (EDA) | ❌ → use `EasyCore.EventBus.Kafka` |
| In-process local events | ❌ → use `EasyCore.EventBus` local bus |

### 1.1 Design Principles

| Principle | Meaning |
|---|---|
| **Infrastructure layer** | Bootstrap, topics, consumer groups, and raw bytes |
| **Standalone** | No dependency on the `EasyCore.EventBus` core package |
| **Adapter-friendly** | The EventBus Kafka adapter reuses this client |

---

## 2. Relation to EventBus

```text
┌─────────────────────────────┐
│  App code (any payload)     │
└──────────────┬──────────────┘
               │ IKafkaClient
               ▼
┌─────────────────────────────┐
│  EasyCore.Kafka (this)      │  ← infrastructure client
└──────────────┬──────────────┘
               │ Confluent.Kafka
               ▼
          Kafka Cluster

┌─────────────────────────────┐
│ EasyCore.EventBus.Kafka     │  ← adapter: IEvent / Handler
│   └── uses this client      │
└─────────────────────────────┘
```

| Package | Role |
|---|---|
| `EasyCore.Kafka` | Generic Kafka client (this package) |
| `EasyCore.EventBus.Kafka` | Maps EventBus `IEvent` onto Kafka |

---

## 3. ⚙ Requirements

| Item | Requirement |
|---|---|
| .NET | 8.0+ |
| Dependency | `Confluent.Kafka` 2.12.x (brought by this package) |
| Broker | Reachable Kafka bootstrap servers |

---

## 4. 📥 Installation

```bash
dotnet add package EasyCore.Kafka
```

---

## 5. ⚡ Quick Start

### 5️⃣.1️⃣ Register DI

```csharp
using EasyCore.Kafka;

builder.Services.EasyCoreKafka(o =>
{
    o.BootstrapServers = "localhost:9092";
    o.TopicName = "EasyCore.Topic";
    o.GroupId = "EasyCore.GroupId";
    o.MessageTimeoutMs = 10000;
    o.RequestTimeoutMs = 10000;
});
```

### 5️⃣.2️⃣ Publish

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

### 5️⃣.3️⃣ Subscribe and Commit

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
                // handle business…

                if (msg.NativeResult is not null)
                    _client.Commit(msg.NativeResult);

                await Task.CompletedTask;
            },
            cancellationToken: stoppingToken);
    }
}
```

`KafkaDeliveredMessage` fields: `Topic`, `Key`, `Body`, `Headers`, `NativeResult` (pass to `Commit`).

---

## 6. 🧩 API Overview

| Member | Description |
|---|---|
| `EasyCoreKafka(Action<KafkaOptions>)` | DI: Options and `IKafkaClient` |
| `ConnectAsync` | Initialize the consumer (idempotent) |
| `PublishAsync(topic, body, key?, headers?)` | Produce to a topic |
| `SubscribeAsync(topics, handler)` | Subscribe and start a background consume loop |
| `Commit(nativeResult)` | Commit the offset for the delivered message |

`IKafkaClient` implements `IAsyncDisposable` — dispose on shutdown.

---

## 7. Options (`KafkaOptions`)

| Property | Type | Default | Description |
|---|---|---|---|
| `BootstrapServers` | `string` | `localhost:9092` | Comma-separated bootstrap addresses |
| `TopicName` | `string` | `EasyCore.Topic` | Topic name prefix (convention / adapter) |
| `GroupId` | `string` | `EasyCore.GroupId` | Consumer group suffix |
| `MessageTimeoutMs` | `int` | `10000` | Produce timeout (ms) |
| `RequestTimeoutMs` | `int` | `10000` | Request timeout (ms) |
| `QueueBufferingMaxMessages` | `int` | `30000` | Producer queue buffer size |
| `AppName` | `string?` | `null` | Used in group naming; entry assembly when null |

---

## 8. ❓ FAQ

**Q: This package vs `EasyCore.EventBus.Kafka`?**  
A: Use the EventBus adapter for `IEvent`, handlers, and retries. Use this package for raw topic I/O.

**Q: What do I pass to `Commit`?**  
A: Pass `KafkaDeliveredMessage.NativeResult` from the handler (the underlying consume result).

**Q: What do `TopicName` / `GroupId` do when used standalone?**  
A: Naming conventions / suffixes. `PublishAsync` / `SubscribeAsync` still use the topics you pass in.

**Q: Multiple bootstrap servers?**  
A: Yes — set `BootstrapServers` to e.g. `kafka1:9092,kafka2:9092`.

---

## 9. 📄 License

MIT OR Apache-2.0 — see the repository [LICENSE](../../LICENSE) if present, or the NuGet package metadata.

---

## 🤝 Contributing

Issues and PRs are welcome. After changing this package, verify `EasyCore.EventBus.Kafka` and related demos.
