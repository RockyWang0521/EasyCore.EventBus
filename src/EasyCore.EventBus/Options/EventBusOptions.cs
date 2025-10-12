using EasyCore.EventBus.Event;

namespace EasyCore.EventBus
{
    /// <summary>
    /// EventBus Options
    /// </summary>
    public class EventBusOptions
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public EventBusOptions()
        {
            Extensions = new List<IEventOptionsExtension>();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Failure retry count
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Failure retry interval (seconds)
        /// </summary>
        public int RetryInterval { get; set; } = 3;

        /// <summary>
        /// Failure callback function
        /// </summary>
        public Action<string, string?>? FailureCallback { get; set; }

        /// <summary>
        /// Interface for event options extensions.
        /// </summary>
        internal IList<IEventOptionsExtension>? Extensions { get; }

#pragma warning disable CS8602 // Converting null literal or possible null value to non-nullable type.
        public void RegisterExtension(IEventOptionsExtension extension)
        {
            Extensions.Add(extension);
        }
#pragma warning restore CS8602 // Converting null literal or possible null value to non-nullable type.
    }
}
