using EasyCore.EventBus;
using EasyCore.EventBus.RabbitMQ;

namespace Web.RabbitMQ;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "EventBus RabbitMQ Subscriber", Version = "v1" });
        });

        var mq = builder.Configuration.GetSection("RabbitMQ");
        builder.Services.AddEasyCoreEventBus(options =>
        {
            options.RetryCount = builder.Configuration.GetValue("EventBus:RetryCount", 3);
            options.RetryInterval = builder.Configuration.GetValue("EventBus:RetryInterval", 3);
            options.FailureCallback = (eventName, payload) =>
                Console.WriteLine($"[FailureCallback] event={eventName} payload={payload}");

            options.RabbitMQ(o =>
            {
                o.HostName = mq["HostName"] ?? "localhost";
                o.UserName = mq["UserName"] ?? "guest";
                o.Password = mq["Password"] ?? "guest";
                o.Port = mq.GetValue("Port", 5672);
                o.VirtualHost = mq["VirtualHost"] ?? "/";
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
            transport = "RabbitMQ",
            tip = "Start Web.RabbitMQ.Publish and POST /api/Publish"
        }));

        app.MapControllers();
        app.Run();
    }
}
