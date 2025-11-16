using EasyCore.EventBus.Event;
using EasyCore.EventBus.Local;
using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.EventBus.Tests
{
    public class LocalEventBusTests
    {
        [Fact]
        public async Task PublishAsync_InvokesAllHandlersInOrder()
        {
            TrackingLocalHandler.CallCount = 0;
            SecondLocalHandler.CallCount = 0;
            TrackingLocalHandler.Messages.Clear();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<ILocalEventHandler<SampleEvent>, TrackingLocalHandler>();
            services.AddTransient<ILocalEventHandler<SampleEvent>, SecondLocalHandler>();
            services.AddSingleton<ILocalEventBus, LocalEventBus>();

            var provider = services.BuildServiceProvider();
            var bus = provider.GetRequiredService<ILocalEventBus>();

            await bus.PublishAsync(new SampleEvent { Message = "hello" });

            Assert.Equal(1, TrackingLocalHandler.CallCount);
            Assert.Equal(1, SecondLocalHandler.CallCount);
            Assert.Equal(new[] { "hello" }, TrackingLocalHandler.Messages);
        }

        [Fact]
        public async Task PublishAsync_PropagatesHandlerException()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<ILocalEventHandler<SampleEvent>, FailingLocalHandler>();
            services.AddSingleton<ILocalEventBus, LocalEventBus>();

            var provider = services.BuildServiceProvider();
            var bus = provider.GetRequiredService<ILocalEventBus>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                bus.PublishAsync(new SampleEvent { Message = "x" }));
        }
    }
}
