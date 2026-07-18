using EasyCore.RabbitMQ;
using Web.Infra.RabbitMQ.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EasyCore.RabbitMQ Infra Demo", Version = "v1" });
});

var rabbit = builder.Configuration.GetSection("RabbitMQ");
builder.Services.EasyCoreRabbitMQ(o =>
{
    o.HostName = rabbit["HostName"] ?? "localhost";
    o.UserName = rabbit["UserName"] ?? "guest";
    o.Password = rabbit["Password"] ?? "guest";
    o.Port = rabbit.GetValue("Port", 5672);
    o.VirtualHost = rabbit["VirtualHost"] ?? "/";
    o.ExchangeName = rabbit["ExchangeName"] ?? "EasyCore.Infra.Demo";
    o.ExchangeType = rabbit["ExchangeType"] ?? "topic";
    o.QueueName = rabbit["QueueName"] ?? "Infra.Queue";
    o.AppName = rabbit["AppName"] ?? "Web.Infra.RabbitMQ";
});

builder.Services.AddHostedService<RabbitMqSubscribeHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
