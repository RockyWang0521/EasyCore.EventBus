# 🐇 EasyCore.RabbitMQ

> **EasyCore.RabbitMQ** is a RabbitMQ infrastructure client for .NET 8. Built on [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client), it provides DI registration, connect, publish, subscribe, and Ack/Nack. It is **not tied** to `IEvent` / EventBus — use it standalone, or via the `EasyCore.EventBus.RabbitMQ` adapter.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ.Client-6.8-orange)
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
- [6. API Overview](#6-api-overview)
- [7. Options (`RabbitMQOptions`)](#7-options-rabbitmqoptions)
- [8. FAQ](#8-faq)
- [9. License](#9-license)

---

## 1. 🎯 Positioning

| Scenario | Fit? |
|---|---|
| Byte-level publish/subscribe against RabbitMQ | ✅ |
| Fine-grained AMQP headers and Ack/Nack | ✅ |
| Unified `IEvent` + handler discovery (EDA) | ❌ → use `EasyCore.EventBus.RabbitMQ` |
| In-process local events | ❌ → use `EasyCore.EventBus` local bus |

### 1.1 Design Principles

| Principle | Meaning |
|---|---|
| **Infrastructure layer** | Connection, topology, and raw bytes — no business event model |
| **Standalone** | No dependency on the `EasyCore.EventBus` core package |
| **Adapter-friendly** | The EventBus RabbitMQ adapter reuses this client |

---

## 2. Relation to EventBus

```text
┌─────────────────────────────┐
│  App code (any payload)     │
└──────────────┬──────────────┘
               │ IRabbitMQClient
               ▼
┌─────────────────────────────┐
│  EasyCore.RabbitMQ (this)   │  ← infrastructure client
└──────────────┬──────────────┘
               │ RabbitMQ.Client
               ▼
          RabbitMQ Broker

┌─────────────────────────────┐
│ EasyCore.EventBus.RabbitMQ  │  ← adapter: IEvent / Handler
│   └── uses this client      │
└─────────────────────────────┘
```

| Package | Role |
|---|---|
| `EasyCore.RabbitMQ` | Generic AMQP client (this package) |
| `EasyCore.EventBus.RabbitMQ` | Maps EventBus `IEvent` onto RabbitMQ |

---

## 3. ⚙ Requirements

| Item | Requirement |
|---|---|
| .NET | 8.0+ |
| Dependency | `RabbitMQ.Client` 6.8.x (brought by this package) |
| Broker | Reachable RabbitMQ instance |

---

## 4. 📥 Installation

```bash
dotnet add package EasyCore.RabbitMQ
```

---

## 5. ⚡ Quick Start

### 5️⃣.1️⃣ Register DI

```csharp
using EasyCore.RabbitMQ;

builder.Services.AddEasyCoreRabbitMQ(o =>
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

### 5️⃣.2️⃣ Publish

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

### 5️⃣.3️⃣ Subscribe with Ack / Nack

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
                    // handle business…
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

`RabbitMQDeliveredMessage` fields: `RoutingKey`, `Body`, `Headers`, `DeliveryTag`, `CorrelationId`.

---

## 6. 🧩 API Overview

| Member | Description |
|---|---|
| `AddEasyCoreRabbitMQ(Action<RabbitMQOptions>)` | DI: Options, connection factory, `IRabbitMQClient` |
| `ConnectAsync` | Connect and declare the configured exchange |
| `PublishAsync(routingKey, body, headers?)` | Publish to the exchange (with confirms) |
| `SubscribeAsync(routingKeys, handler)` | Declare queue, bind keys, start consuming |
| `Ack(deliveryTag)` | Acknowledge |
| `Nack(deliveryTag, requeue)` | Reject; requeue when `requeue` is `true` |

`IRabbitMQClient` implements `IAsyncDisposable` — dispose on shutdown.

---

## 7. Options (`RabbitMQOptions`)

| Property | Type | Default | Description |
|---|---|---|---|
| `HostName` | `string` | `localhost` | Host; comma-separated for clusters |
| `UserName` | `string` | `guest` | Username |
| `Password` | `string` | `guest` | Password |
| `Port` | `int` | `5672` | AMQP port |
| `ExchangeName` | `string` | `EasyCore.EventBus` | Exchange name |
| `QueueName` | `string` | `EasyCore.Queue` | Queue name suffix (combined with AppName) |
| `ExchangeType` | `string` | `topic` | `topic` / `direct` / `fanout` / `headers` |
| `VirtualHost` | `string` | `/` | Virtual host |
| `MessageTTL` | `int` | `864000000` | Queue message TTL (ms, ~10 days) |
| `QueueMode` | `string?` | `null` | Optional, e.g. `lazy` |
| `Durable` | `bool` | `true` | Durable queue |
| `Exclusive` | `bool` | `false` | Exclusive queue |
| `AutoDelete` | `bool` | `false` | Auto-delete when unused |
| `QueueType` | `string?` | `null` | Optional, e.g. `quorum` |
| `AppName` | `string?` | `null` | Queue name prefix; entry assembly when null |

---

## 8. ❓ FAQ

**Q: This package vs `EasyCore.EventBus.RabbitMQ`?**  
A: Use the EventBus adapter for `IEvent`, handlers, and retries. Use this package for raw byte publish/subscribe.

**Q: Must I call `ConnectAsync` first?**  
A: Yes — connect before publish or subscribe so the connection and exchange are ready.

**Q: How is the subscription queue named?**  
A: From `AppName` (or entry assembly) plus `QueueName`, then bound to the given routing keys.

**Q: When to requeue on `Nack`?**  
A: Transient failures: `requeue: true`. Poison messages: `false` (or dead-letter) to avoid loops.

**Q: Multiple hosts?**  
A: Pass comma-separated hosts in `HostName`; failover follows RabbitMQ.Client behavior.

---

## 9. 📄 License

MIT — see the repository [LICENSE](../../LICENSE), or the NuGet package metadata.

---

## 🤝 Contributing

Issues and PRs are welcome. After changing this package, verify `EasyCore.EventBus.RabbitMQ` and related demos.
