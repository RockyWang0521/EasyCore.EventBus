using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Tests
{
    public class RetryHeaderParsingTests
    {
        [Fact]
        public void ParseRetryHeaders_FromStringMap()
        {
            var defaults = new EventBusOptions { RetryCount = 3, RetryInterval = 3 };
            var headers = new Dictionary<string, string>
            {
                [EventMessageHeaders.Retry] = "5",
                [EventMessageHeaders.RetryInterval] = "2"
            };

            var (maxRetry, interval) = DistributedEventDispatcher.ParseRetryHeaders(headers, defaults);
            Assert.Equal(5, maxRetry);
            Assert.Equal(2, interval);
        }

        [Fact]
        public void ParseRetryHeaders_FromObjectMap()
        {
            var defaults = new EventBusOptions { RetryCount = 3, RetryInterval = 3 };
            var headers = new Dictionary<string, object>
            {
                [EventMessageHeaders.Retry] = 7,
                [EventMessageHeaders.RetryInterval] = 4
            };

            var (maxRetry, interval) = DistributedEventDispatcher.ParseRetryHeaders(headers, defaults);
            Assert.Equal(7, maxRetry);
            Assert.Equal(4, interval);
        }
    }
}