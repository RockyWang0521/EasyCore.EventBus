namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Well-known message header names used across transports.
    /// </summary>
    public static class EventMessageHeaders
    {
        /// <summary>
        /// Header that carries the event CLR type name.
        /// </summary>
        public const string EventType = "EventType";

        /// <summary>
        /// Header that carries the maximum retry count for the message.
        /// </summary>
        public const string Retry = "x-retry";

        /// <summary>
        /// Header that carries the retry interval in seconds.
        /// </summary>
        public const string RetryInterval = "x-retry-time";
    }
}
