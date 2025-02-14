using EasyCore.EventBus.Event;
using Microsoft.Extensions.Hosting;

namespace EasyCore.EventBus.HostedService
{
    public class EventBusHostedService : IHostedService
    {
        private readonly IEventRabbitMQClient _eventMQClient;

        public EventBusHostedService(IEventRabbitMQClient eventRabbitMQClient)
        {
            _eventMQClient = eventRabbitMQClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _eventMQClient.Create();

            _eventMQClient.Subscribe();

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
