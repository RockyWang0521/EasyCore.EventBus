using EasyCore.EventBus.Event;

namespace EasyCore.EventBus
{
    /// <summary>
    /// EventBus options shared across local and distributed transports.
    /// </summary>
    public class EventBusOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EventBusOptions"/> with an empty extension list.
        /// </summary>
        public EventBusOptions()
        {
            Extensions = new List<IEventOptionsExtension>();
        }

        /// <summary>
        /// Failure retry count (additional attempts after the first try).
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Failure retry interval in seconds.
        /// </summary>
        public int RetryInterval { get; set; } = 3;

        /// <summary>
        /// Invoked when retries are exhausted.
        /// </summary>
        /// <remarks>
        /// The first argument is the event type name; the second is the optional payload (e.g. JSON).
        /// </remarks>
        public Action<string, string?>? FailureCallback { get; set; }

        /// <summary>
        /// Registered transport extensions applied during DI setup.
        /// </summary>
        internal IList<IEventOptionsExtension> Extensions { get; }

        /// <summary>
        /// Registers a transport extension.
        /// </summary>
        /// <param name="extension">The extension that adds transport-specific services.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="extension"/> is <c>null</c>.</exception>
        public void RegisterExtension(IEventOptionsExtension extension)
        {
            ArgumentNullException.ThrowIfNull(extension);
            Extensions.Add(extension);
        }
    }
}
