using EasyCore.EventBus.Event;
using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.EventBus.Tests
{
    public class HandlerRegistrationTests
    {
        [Fact]
        public void EasyCoreEventBus_RegistersHandlersFromLoadedAssemblies()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.EasyCoreEventBus();

            var provider = services.BuildServiceProvider();
            var localHandlers = provider.GetServices<ILocalEventHandler<SampleEvent>>().ToList();
            var distributedHandlers = provider.GetServices<IDistributedEventHandler<SampleEvent>>().ToList();

            Assert.NotEmpty(localHandlers);
            Assert.NotEmpty(distributedHandlers);
            Assert.Contains(localHandlers, h => h is TrackingLocalHandler);
            Assert.Contains(distributedHandlers, h => h is TrackingDistributedHandler);
        }

        [Fact]
        public void EventTypeScanner_FindsDistributedEventTypes()
        {
            var types = EventTypeScanner.GetDistributedEventTypes();
            Assert.Contains(types, t => t == typeof(SampleEvent));
        }
    }
}
