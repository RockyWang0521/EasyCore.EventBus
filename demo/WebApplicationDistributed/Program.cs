using EasyCore.EventBus;
using EasyCore.EventBus.RabbitMQ;

namespace WebApplicationDistributed
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddAppEventBus(options =>
            {
                options.RabbitMQ(opt =>
                {
                    opt.HostName = "192.168.157.142";
                    opt.UserName = "123";
                    opt.Password = "123";
                    opt.Port = 5672;
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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
