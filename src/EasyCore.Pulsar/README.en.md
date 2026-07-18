# ⚡ EasyCore.Pulsar

> **EasyCore.Pulsar** is an Apache Pulsar infrastructure client for .NET 8. Built on [Pulsar.Client](https://www.nuget.org/packages/Pulsar.Client), it provides DI registration, connect, topic publish/subscribe, and `AcknowledgeAsync`. It is **not tied** to `IEvent` / EventBus — use it standalone, or via the `EasyCore.EventBus.Pulsar` adapter.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![Pulsar](https://img.shields.io/badge/Pulsar.Client-3.12-orange)
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
- [7. Options (`PulsarOptions`)](#7-options-pulsaroptions)
- [8. FAQ](#8-faq)
- [9. License](#9-license)

---

## 1. 🎯 Positioning

| Scenario | Fit? |
|---|---|
| Byte-level publish/subscribe on Pulsar topics | ✅ |
| Properties and MessageId acknowledgment | ✅ |
| TLS / authentication client options | ✅ |
| Unified `IEvent` + handler discovery (EDA) | ❌ → use `EasyCore.EventBus.Pulsar` |
| In-process local events | ❌ → use `EasyCore.EventBus` local bus |

### 1.1 Design Principles

| Principle | Meaning |
|---|---|
| **Infrastructure layer** | ServiceUrl, topic prefix, TLS, and raw bytes |
| **Standalone** | No dependency on the `EasyCore.EventBus` core package |
| **Adapter-friendly** | The EventBus Pulsar adapter reuses this client |

---

## 2. Relation to EventBus

```text
┌─────────────────────────────┐
│  App code (any payload)     │
└──────────────┬──────────────┘
               │ IPulsarClient
               ▼
┌─────────────────────────────┐
│  EasyCore.Pulsar (this)     │  ← infrastructure client
└──────────────┬──────────────┘
               │ Pulsar.Client
               ▼
          Pulsar Cluster

┌─────────────────────────────┐
│ EasyCore.EventBus.Pulsar    │  ← adapter: IEvent / Handler
│   └── uses this client      │
└─────────────────────────────┘
```

| Package | Role |
|---|---|
| `EasyCore.Pulsar` | Generic Pulsar client (this package) |
| `EasyCore.EventBus.Pulsar` | Maps EventBus `IEvent` onto Pulsar |

---

## 3. ⚙ Requirements

| Item | Requirement |
|---|---|
| .NET | 8.0+ |
| Dependency | `Pulsar.Client` 3.12.x (brought by this package) |
| Broker | Reachable Pulsar service URL (e.g. `pulsar://localhost:6650`) |

---

## 4. 📥 Installation

```bash
dotnet add package EasyCore.Pulsar
```

---

## 5. ⚡ Quick Start

### 5️⃣.1️⃣ Register DI

```csharp
using EasyCore.Pulsar;

builder.Services.EasyCorePulsar(o =>
{
    o.ServiceUrl = "pulsar://localhost:6650";
    o.TopicPrefix = "persistent://public/default/";
    o.AppName = "MyApp";
    // TLS / auth as needed:
    // o.UseTls = true;
    // o.TlsAllowInsecureConnection = false;
});
```

### 5️⃣.2️⃣ Publish

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

        // Relative topic names are prefixed with TopicPrefix
        await _client.PublishAsync(
            topic: "invoice.created",
            body: body,
            properties: properties,
            cancellationToken: ct);
    }
}
```

### 5️⃣.3️⃣ Subscribe and Acknowledge

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
                // handle business…

                await _client.AcknowledgeAsync(msg.MessageId, ct);
            },
            cancellationToken: stoppingToken);
    }
}
```

`PulsarDeliveredMessage` fields: `Topic`, `Body`, `Properties`, `MessageId`.

---

## 6. 🧩 API Overview

| Member | Description |
|---|---|
| `EasyCorePulsar(Action<PulsarOptions>)` | DI: Options and `IPulsarClient` |
| `ConnectAsync` | Connect to ServiceUrl and build the native client |
| `PublishAsync(topic, body, properties?)` | Publish; relative topics get `TopicPrefix` |
| `SubscribeAsync(topics, handler)` | Subscribe and start a background consume loop |
| `AcknowledgeAsync(messageId)` | Acknowledge by Pulsar `MessageId` |

`IPulsarClient` implements `IAsyncDisposable` — dispose on shutdown.

---

## 7. Options (`PulsarOptions`)

| Property | Type | Default | Description |
|---|---|---|---|
| `ServiceUrl` | `string` | `pulsar://localhost:6650` | Pulsar service URL |
| `EnableClientLog` | `bool` | `false` | Enable native client logging |
| `UseTls` | `bool` | library default | Enable TLS |
| `TlsHostnameVerificationEnable` | `bool` | library default | TLS hostname verification |
| `TlsAllowInsecureConnection` | `bool` | library default | Allow insecure TLS |
| `TlsTrustCertificate` | `X509Certificate2` | library default | Trust certificate |
| `Authentication` | `Authentication` | library default | Pulsar auth plugin/config |
| `TlsProtocols` | `SslProtocols` | library default | Allowed TLS protocols |
| `TopicPrefix` | `string` | `persistent://public/default/` | Prefix for relative topic names |
| `AppName` | `string?` | `null` | Subscription/consumer naming; entry assembly when null |

> TLS / Authentication defaults come from `PulsarClientConfiguration.Default`.

---

## 8. ❓ FAQ

**Q: This package vs `EasyCore.EventBus.Pulsar`?**  
A: Use the EventBus adapter for `IEvent`, handlers, and retries. Use this package for raw topic I/O.

**Q: Must topics be fully qualified?**  
A: Relative names are fine (prefixed with `TopicPrefix`), or use full `persistent://tenant/ns/topic`.

**Q: Must I call `ConnectAsync` first?**  
A: Yes — connect before publish or subscribe so the native client is built.

**Q: How do I enable TLS?**  
A: Set `UseTls = true` and configure certificates, hostname verification, and `Authentication` as needed.

---

## 9. 📄 License

MIT OR Apache-2.0 — see the repository [LICENSE](../../LICENSE) if present, or the NuGet package metadata.

---

## 🤝 Contributing

Issues and PRs are welcome. After changing this package, verify `EasyCore.EventBus.Pulsar` and related demos.
