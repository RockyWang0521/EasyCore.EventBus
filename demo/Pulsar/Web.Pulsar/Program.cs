using EasyCore.EventBus;
using EasyCore.EventBus.Pulsar;

namespace Web.Pulsar;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "EventBus Pulsar Subscriber", Version = "v1" });
        });

        var pulsar = builder.Configuration.GetSection("Pulsar");
        builder.Services.AddEasyCoreEventBus(options =>
        {
            options.RetryCount = builder.Configuration.GetValue("EventBus:RetryCount", 3);
            options.RetryInterval = builder.Configuration.GetValue("EventBus:RetryInterval", 3);
            options.FailureCallback = (eventName, payload) =>
                Console.WriteLine($"[FailureCallback] event={eventName} payload={payload}");

            options.Pulsar(o =>
            {
                o.ServiceUrl = pulsar["ServiceUrl"] ?? "pulsar://localhost:6650";
                o.TopicPrefix = pulsar["TopicPrefix"] ?? "persistent://public/default/";
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
            transport = "Pulsar",
            tip = "Start Web.Pulsar.Publish and POST /api/Publish"
        }));

        app.MapControllers();
        app.Run();
    }
}
