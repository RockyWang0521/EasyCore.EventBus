using EasyCore.RedisStreams;
using System.Text.Json;

namespace EasyCore.EventBus.Tests
{
    public class RedisStreamMessageFormatTests
    {
        [Fact]
        public void SerializeAndDeserializeHeader_RoundTrips()
        {
            var header = new RedisStreamHeader { RetryCount = 3, RetryInterval = 5 };
            var json = RedisStreamMessageFormat.SerializeHeader(header);
            var restored = RedisStreamMessageFormat.DeserializeHeader(json);

            Assert.NotNull(restored);
            Assert.Equal(3, restored!.RetryCount);
            Assert.Equal(5, restored.RetryInterval);
        }

        [Fact]
        public void MessageFieldNames_AreConsistentForSyncAndAsyncPublish()
        {
            // Both publish paths must use the same field names (regression for old headers/message mismatch).
            Assert.Equal("RedisHeader", RedisStreamMessageFormat.HeaderField);
            Assert.Equal("payload", RedisStreamMessageFormat.PayloadField);
            Assert.Equal("type", RedisStreamMessageFormat.TypeField);

            var headerJson = RedisStreamMessageFormat.SerializeHeader(new RedisStreamHeader { RetryCount = 1, RetryInterval = 2 });
            var payload = JsonSerializer.Serialize(new { Message = "x" });

            var fields = new Dictionary<string, string>
            {
                [RedisStreamMessageFormat.HeaderField] = headerJson,
                [RedisStreamMessageFormat.TypeField] = "SampleEvent",
                [RedisStreamMessageFormat.PayloadField] = payload
            };

            Assert.True(fields.ContainsKey(RedisStreamMessageFormat.HeaderField));
            Assert.True(fields.ContainsKey(RedisStreamMessageFormat.TypeField));
            Assert.True(fields.ContainsKey(RedisStreamMessageFormat.PayloadField));
            Assert.False(fields.ContainsKey("headers"));
            Assert.False(fields.ContainsKey("message"));
        }
    }
}
