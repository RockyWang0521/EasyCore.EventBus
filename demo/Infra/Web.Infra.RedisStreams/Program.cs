using EasyCore.RedisStreams;
using Web.Infra.RedisStreams.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EasyCore.RedisStreams Infra Demo", Version = "v1" });
});

var redis = builder.Configuration.GetSection("RedisStreams");
builder.Services.AddEasyCoreRedisStreams(o =>
{
    o.EndPoints = redis.GetSection("EndPoints").Get<List<string>>() ?? new List<string> { "localhost:6379" };
    o.Password = redis["Password"];
    o.User = redis["User"];
    o.DefaultDatabase = redis.GetValue("DefaultDatabase", 0);
    o.ConsumerGroup = redis["ConsumerGroup"] ?? "Infra.Group";
    o.AppName = redis["AppName"] ?? "Web.Infra.RedisStreams";
    o.AbortOnConnectFail = redis.GetValue("AbortOnConnectFail", false);
});

builder.Services.AddHostedService<RedisSubscribeHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
