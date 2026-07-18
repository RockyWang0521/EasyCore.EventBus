using EasyCore.EventBus;
using EasyCore.EventBus.Kafka;

namespace Web.Kafka.Publish;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "EventBus Kafka Publisher", Version = "v1" });
        });

        var kafka = builder.Configuration.GetSection("Kafka");
        builder.Services.AddEasyCoreEventBus(options =>
        {
            options.RetryCount = builder.Configuration.GetValue("EventBus:RetryCount", 3);
            options.RetryInterval = builder.Configuration.GetValue("EventBus:RetryInterval", 3);

            options.Kafka(o =>
            {
                o.BootstrapServers = kafka["BootstrapServers"] ?? "localhost:9092";
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
            role = "EventBus.Publisher",
            transport = "Kafka",
            endpoints = new[] { "POST /api/Publish", "POST /api/Publish/one", "POST /api/Publish/batch" }
        }));

        app.MapControllers();
        app.Run();
    }
}
