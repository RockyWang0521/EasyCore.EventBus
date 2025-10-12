using EasyCore.EventBus;
using EasyCore.EventBus.RabbitMQ;

namespace WebApplicationDistributed
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.EasyCoreEventBus(options =>
            {
                options.RabbitMQ(opt =>
                {
                    opt.HostName = "localhost";
                    opt.UserName = "123";
                    opt.Password = "123";
                    opt.Port = 5672;
                    opt.ExchangeName = "easy-core-event-bus";
                    opt.QueueName = "easy-core-event-bus";
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
