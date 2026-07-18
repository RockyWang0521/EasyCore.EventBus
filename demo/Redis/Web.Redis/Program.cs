using EasyCore.EventBus;
using EasyCore.EventBus.RedisStreams;

namespace Web.Redis;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "EventBus RedisStreams Subscriber", Version = "v1" });
        });

        var redis = builder.Configuration.GetSection("RedisStreams");
        builder.Services.AddEasyCoreEventBus(options =>
        {
            options.RetryCount = builder.Configuration.GetValue("EventBus:RetryCount", 3);
            options.RetryInterval = builder.Configuration.GetValue("EventBus:RetryInterval", 3);
            options.FailureCallback = (eventName, payload) =>
                Console.WriteLine($"[FailureCallback] event={eventName} payload={payload}");

            options.RedisStreams(o =>
            {
                o.EndPoints = redis.GetSection("EndPoints").Get<List<string>>()
                    ?? new List<string> { "localhost:6379" };
                o.Password = redis["Password"];
                o.DefaultDatabase = redis.GetValue("DefaultDatabase", 0);
                o.ConsumerGroup = redis["ConsumerGroup"] ?? "RedisGroup";
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapGet("/", () => Results.Ok(new
        {
            role = "EventBus.Subscriber",
            transport = "RedisStreams",
            tip = "Start Web.Redis.Publish and POST /api/Publish"
        }));

        app.MapControllers();
        app.Run();
    }
}
