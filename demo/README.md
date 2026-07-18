# EasyCore.EventBus Demo 总览

仓库提供两类示例：

1. **EventBus 分布式总线**（`IDistributedEventBus` + Handler）
2. **基础设施客户端独立使用**（仅 `EasyCore.RabbitMQ` / `Kafka` / `Pulsar` / `RedisStreams`）

## 1. EventBus 分布式 Demo

成对启动 **Subscribe** + **Publish**，在 Publish 的 Swagger 调用 `POST /api/Publish`。

| 目录 | Subscribe | Publish | 说明 |
|---|---|---|---|
| [`RabbitMq/`](RabbitMq/) | `Web.RabbitMQ` | `Web.RabbitMQ.Publish` | RabbitMQ + 重试 / FailureCallback |
| [`Kafka/`](Kafka/) | `Web.Kafka` | `Web.Kafka.Publish` | Kafka |
| [`Pulsar/`](Pulsar/) | `Web.Pulsar` | `Web.Pulsar.Publish` | Pulsar |
| [`Redis/`](Redis/) | `Web.Redis` | `Web.Redis.Publish` | Redis Streams |
| [`Winform/`](Winform/) | WinForms / Web | — | 本地事件 + 分布式 WinForms |

Publish 常用接口：

- `POST /api/Publish` — 发布全部示例事件（可传 `{ "message": "..." }`）
- `POST /api/Publish/one` — 单条
- `POST /api/Publish/batch` — 批量 `{ "count": 10, "message": "..." }`

连接与重试见各项目 `appsettings.json`（`EventBus:RetryCount` / Broker 段）。

```bash
dotnet run --project demo/RabbitMq/Web.RabbitMQ
dotnet run --project demo/RabbitMq/Web.RabbitMQ.Publish
```

## 2. Infra 独立客户端 Demo

不引用 EventBus，直接测底层 Publish / Subscribe：

| 项目 | 端口 | 包 |
|---|---|---|
| [`Infra/Web.Infra.RabbitMQ`](Infra/Web.Infra.RabbitMQ/) | 5301 | EasyCore.RabbitMQ |
| [`Infra/Web.Infra.Kafka`](Infra/Web.Infra.Kafka/) | 5302 | EasyCore.Kafka |
| [`Infra/Web.Infra.Pulsar`](Infra/Web.Infra.Pulsar/) | 5303 | EasyCore.Pulsar |
| [`Infra/Web.Infra.RedisStreams`](Infra/Web.Infra.RedisStreams/) | 5304 | EasyCore.RedisStreams |

```bash
dotnet run --project demo/Infra/Web.Infra.RabbitMQ
# Swagger → POST /api/messages 或 /api/messages/ping
```

详见 [`Infra/README.md`](Infra/README.md)。
