using EasyCore.EventBus.Event;

namespace EasyCore.EventBus.Tests
{
    public sealed class SampleEvent : IEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    public sealed class TrackingLocalHandler : ILocalEventHandler<SampleEvent>
    {
        public static int CallCount;
        public static List<string> Messages { get; } = new();

        public Task HandleAsync(SampleEvent eventMessage)
        {
            CallCount++;
            Messages.Add(eventMessage.Message);
            return Task.CompletedTask;
        }
    }

    public sealed class SecondLocalHandler : ILocalEventHandler<SampleEvent>
    {
        public static int CallCount;

        public Task HandleAsync(SampleEvent eventMessage)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    public sealed class FailingLocalHandler : ILocalEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent eventMessage) =>
            throw new InvalidOperationException("local failed");
    }

    public sealed class TrackingDistributedHandler : IDistributedEventHandler<SampleEvent>
    {
        public static int CallCount;
        public static int FailUntilAttempt;

        public Task HandleAsync(SampleEvent eventMessage)
        {
            CallCount++;
            if (CallCount <= FailUntilAttempt)
                throw new InvalidOperationException("distributed failed");

            return Task.CompletedTask;
        }
    }

    public sealed class SecondDistributedHandler : IDistributedEventHandler<SampleEvent>
    {
        public static int CallCount;

        public Task HandleAsync(SampleEvent eventMessage)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
