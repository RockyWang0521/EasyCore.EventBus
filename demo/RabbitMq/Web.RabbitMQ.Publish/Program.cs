using EasyCore.EventBus;
using EasyCore.EventBus.RabbitMQ;

namespace Web.RabbitMQ.Publish;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "EventBus RabbitMQ Publisher", Version = "v1" });
        });

        var mq = builder.Configuration.GetSection("RabbitMQ");
        builder.Services.EasyCoreEventBus(options =>
        {
            options.RetryCount = builder.Configuration.GetValue("EventBus:RetryCount", 3);
            options.RetryInterval = builder.Configuration.GetValue("EventBus:RetryInterval", 3);

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
            role = "EventBus.Publisher",
            transport = "RabbitMQ",
            endpoints = new[] { "POST /api/Publish", "POST /api/Publish/one", "POST /api/Publish/batch" }
        }));

        app.MapControllers();
        app.Run();
    }
}
