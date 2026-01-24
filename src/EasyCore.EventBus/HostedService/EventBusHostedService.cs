using EasyCore.EventBus.Event;
using Microsoft.Extensions.Hosting;

namespace EasyCore.EventBus.HostedService
{
    public class EventBusHostedService : IHostedService
    {
        private readonly IEventMessageQueueClient _MessageQueueClient;

        public EventBusHostedService(IEventMessageQueueClient MessageQueueClient) => _MessageQueueClient = MessageQueueClient;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _MessageQueueClient.Connect();

            _MessageQueueClient.Subscribe();

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken) => await Task.CompletedTask;
    }
}
