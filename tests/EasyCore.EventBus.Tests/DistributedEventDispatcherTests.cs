using EasyCore.EventBus.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EasyCore.EventBus.Tests
{
    public class DistributedEventDispatcherTests
    {
        [Fact]
        public async Task DispatchAsync_InvokesMultipleHandlers()
        {
            TrackingDistributedHandler.CallCount = 0;
            TrackingDistributedHandler.FailUntilAttempt = 0;
            SecondDistributedHandler.CallCount = 0;

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<IDistributedEventHandler<SampleEvent>, TrackingDistributedHandler>();
            services.AddTransient<IDistributedEventHandler<SampleEvent>, SecondDistributedHandler>();

            var provider = services.BuildServiceProvider();
            var options = Options.Create(new EventBusOptions { RetryCount = 0, RetryInterval = 0 });
            var dispatcher = new DistributedEventDispatcher(provider, options);

            var json = JsonSerializer.Serialize(new SampleEvent { Message = "m" });
            var result = await dispatcher.DispatchAsync(typeof(SampleEvent), json, maxRetry: 0, retryIntervalSeconds: 0);

            Assert.Equal(EventDispatchResult.Handled, result);
            Assert.Equal(1, TrackingDistributedHandler.CallCount);
            Assert.Equal(1, SecondDistributedHandler.CallCount);
        }

        [Fact]
        public async Task DispatchAsync_RetriesThenSucceeds()
        {
            TrackingDistributedHandler.CallCount = 0;
            TrackingDistributedHandler.FailUntilAttempt = 2;

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<IDistributedEventHandler<SampleEvent>, TrackingDistributedHandler>();

            var provider = services.BuildServiceProvider();
            var options = Options.Create(new EventBusOptions());
            var dispatcher = new DistributedEventDispatcher(provider, options);

            var json = JsonSerializer.Serialize(new SampleEvent { Message = "m" });
            var result = await dispatcher.DispatchAsync(typeof(SampleEvent), json, maxRetry: 3, retryIntervalSeconds: 0);

            Assert.Equal(EventDispatchResult.Handled, result);
            Assert.Equal(3, TrackingDistributedHandler.CallCount);
        }

        [Fact]
        public async Task DispatchAsync_InvokesFailureCallbackWhenRetriesExhausted()
        {
            TrackingDistributedHandler.CallCount = 0;
            TrackingDistributedHandler.FailUntilAttempt = 100;

            string? callbackKey = null;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<IDistributedEventHandler<SampleEvent>, TrackingDistributedHandler>();

            var provider = services.BuildServiceProvider();
            var options = Options.Create(new EventBusOptions
            {
                FailureCallback = (key, _) => callbackKey = key
            });
            var dispatcher = new DistributedEventDispatcher(provider, options);

            var json = JsonSerializer.Serialize(new SampleEvent { Message = "m" });
            var result = await dispatcher.DispatchAsync(typeof(SampleEvent), json, maxRetry: 1, retryIntervalSeconds: 0);

            Assert.Equal(EventDispatchResult.RetryExhausted, result);
            Assert.Equal(nameof(SampleEvent), callbackKey);
        }

        [Fact]
        public async Task DispatchAsync_ReturnsNoHandlerWhenNoneRegistered()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var dispatcher = new DistributedEventDispatcher(provider, Options.Create(new EventBusOptions()));

            var json = JsonSerializer.Serialize(new SampleEvent { Message = "m" });
            var result = await dispatcher.DispatchAsync(typeof(SampleEvent), json, 0, 0);

            Assert.Equal(EventDispatchResult.NoHandler, result);
        }
    }
}
