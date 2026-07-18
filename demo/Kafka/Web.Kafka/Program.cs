using EasyCore.EventBus;
using EasyCore.EventBus.Kafka;

namespace Web.Kafka;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "EventBus Kafka Subscriber", Version = "v1" });
        });

        var kafka = builder.Configuration.GetSection("Kafka");
        builder.Services.AddEasyCoreEventBus(options =>
        {
            options.RetryCount = builder.Configuration.GetValue("EventBus:RetryCount", 3);
            options.RetryInterval = builder.Configuration.GetValue("EventBus:RetryInterval", 3);
            options.FailureCallback = (eventName, payload) =>
                Console.WriteLine($"[FailureCallback] event={eventName} payload={payload}");

            options.Kafka(o =>
            {
                o.BootstrapServers = kafka["BootstrapServers"] ?? "localhost:9092";
                o.GroupId = kafka["GroupId"] ?? "EasyCore.GroupId";
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
            transport = "Kafka",
            tip = "Start Web.Kafka.Publish and POST /api/Publish"
        }));

        app.MapControllers();
        app.Run();
    }
}
