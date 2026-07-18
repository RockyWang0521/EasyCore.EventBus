# Infra 独立客户端 Demo

本目录演示 **仅引用基础设施包**（不经过 EventBus）的发布 / 订阅用法，便于单独验证 Broker 连通性。

| 项目 | 包 | 默认端口 | 说明 |
|---|---|---|---|
| `Web.Infra.RabbitMQ` | `EasyCore.RabbitMQ` | `5301` | `IRabbitMQClient` Publish / Subscribe / Ack |
| `Web.Infra.Kafka` | `EasyCore.Kafka` | `5302` | `IKafkaClient` Publish / Subscribe / Commit |
| `Web.Infra.Pulsar` | `EasyCore.Pulsar` | `5303` | `IPulsarClient` Publish / Subscribe / Acknowledge |
| `Web.Infra.RedisStreams` | `EasyCore.RedisStreams` | `5304` | `IRedisStreamsClient` Publish / Subscribe / Ack |

## 快速开始

1. 启动对应中间件（RabbitMQ / Kafka / Pulsar / Redis）。
2. 按需修改各项目 `appsettings.json` 中的连接信息。
3. 运行项目，打开 Swagger：

```bash
dotnet run --project demo/Infra/Web.Infra.RabbitMQ
dotnet run --project demo/Infra/Web.Infra.Kafka
dotnet run --project demo/Infra/Web.Infra.Pulsar
dotnet run --project demo/Infra/Web.Infra.RedisStreams
```

4. 调用 `POST /api/messages` 发布；订阅由后台 `HostedService` 自动启动，日志中可见收到的消息。

> EventBus 分布式示例仍在 `demo/RabbitMq`、`demo/Kafka`、`demo/Pulsar`、`demo/Redis`、`demo/Winform`。
