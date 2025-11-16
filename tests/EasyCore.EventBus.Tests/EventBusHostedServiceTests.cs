using EasyCore.EventBus.Event;
using EasyCore.EventBus.HostedService;
using NSubstitute;

namespace EasyCore.EventBus.Tests
{
    public class EventBusHostedServiceTests
    {
        [Fact]
        public async Task StartAsync_ConnectsAndSubscribes()
        {
            var client = Substitute.For<IEventMessageQueueClient>();
            client.ConnectAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            client.SubscribeAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            client.DisposeAsync().Returns(ValueTask.CompletedTask);

            var hosted = new EventBusHostedService(client);
            await hosted.StartAsync(CancellationToken.None);

            await client.Received(1).ConnectAsync(Arg.Any<CancellationToken>());
            await client.Received(1).SubscribeAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StopAsync_DisposesClient()
        {
            var client = Substitute.For<IEventMessageQueueClient>();
            client.ConnectAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            client.SubscribeAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            client.DisposeAsync().Returns(ValueTask.CompletedTask);

            var hosted = new EventBusHostedService(client);
            await hosted.StartAsync(CancellationToken.None);
            await hosted.StopAsync(CancellationToken.None);

            await client.Received(1).DisposeAsync();
        }
    }
}
