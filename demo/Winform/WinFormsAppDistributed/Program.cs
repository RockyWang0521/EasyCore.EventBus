using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EasyCore.EventBus;
using EasyCore.EventBus.RabbitMQ;
using Newtonsoft.Json;

namespace WinFormsAppDistributed
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

                services.EasyCoreEventBus(options =>
                {
                    options.RabbitMQ(opt =>
                    {
                        opt.HostName = "localhost";
                        opt.UserName = "123";
                        opt.Password = "123";
                        opt.Port = 5672;
                    });

                    options.FailureCallback = (key, mes) =>
                    {
                        MessageBox.Show(mes, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                    };
                });
            });
    }
}