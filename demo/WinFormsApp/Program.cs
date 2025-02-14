using Microsoft.Extensions.Hosting;
using EasyCore.EventBus;
using EasyCore.EventBus.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WinFormsApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var host = CreateHostBuilder().Build();

            ApplicationConfiguration.Initialize();

            var mainForm = host.Services.GetRequiredService<Main>();

            var backgroundService = host.Services.GetRequiredService<IHostedService>();

            backgroundService.StartAsync(default).Wait();

            Application.Run(mainForm);
        }

        public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<Main>();

                services.AddAppEventBus(options =>
                {
                    options.RabbitMQ(opt =>
                    {
                        opt.HostName = "192.168.157.142";
                        opt.UserName = "123";
                        opt.Password = "123";
                        opt.Port = 5672;
                    });
                });
            });
    }
}