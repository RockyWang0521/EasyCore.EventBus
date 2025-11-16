namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Result of dispatching a distributed event to handlers.
    /// </summary>
    public enum EventDispatchResult
    {
        /// <summary>
        /// Handlers completed successfully.
        /// </summary>
        Handled = 0,

        /// <summary>
        /// No handler was registered for the event type.
        /// </summary>
        NoHandler = 1,

        /// <summary>
        /// Event type could not be resolved.
        /// </summary>
        UnknownType = 2,

        /// <summary>
        /// All retries failed; failure callback was invoked.
        /// </summary>
        RetryExhausted = 3
    }
}
