namespace EasyCore.EventBus.RedisStreams
{
    public static class EventBusRedisStreamsExtensions
    {
        public static EventBusOptions RedisStreams(this EventBusOptions options, List<string> endPoints)
        {
            if (endPoints == null || endPoints.Count == 0)
                throw new ArgumentException("At least one endpoint is required.", nameof(endPoints));

            return options.RedisStreams(opt => opt.EndPoints = endPoints);
        }

        public static EventBusOptions RedisStreams(this EventBusOptions options, Action<RedisStreamsOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            options.RegisterExtension(new RedisStreamsOptionsExtension(configure));
            return options;
        }
    }
}
