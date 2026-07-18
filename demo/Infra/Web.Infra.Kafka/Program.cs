using EasyCore.Kafka;
using Web.Infra.Kafka.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EasyCore.Kafka Infra Demo", Version = "v1" });
});

var kafka = builder.Configuration.GetSection("Kafka");
builder.Services.EasyCoreKafka(o =>
{
    o.BootstrapServers = kafka["BootstrapServers"] ?? "localhost:9092";
    o.GroupId = kafka["GroupId"] ?? "Infra.GroupId";
    o.AppName = kafka["AppName"] ?? "Web.Infra.Kafka";
    o.MessageTimeoutMs = kafka.GetValue("MessageTimeoutMs", 10000);
});

builder.Services.AddHostedService<KafkaSubscribeHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
