namespace EasyCore.EventBus.RedisStreams
{
    public static class EventBusRedisStreamsExtensions
    {
        public static EventBusOptions RedisStreams(this EventBusOptions options, List<string> EndPoints)
        {
            if (EndPoints.Count <= 0) throw new ArgumentException(nameof(EndPoints));

            var configure = new Action<RedisStreamsOptions>(options =>
            {
                options.EndPoints = EndPoints;
            });

            options.RegisterExtension(new RedisStreamsOptionsExtension(configure));

            return options;
        }

        public static EventBusOptions RedisStreams(this EventBusOptions options, Action<RedisStreamsOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var rabbitMQOptions = new RedisStreamsOptions();

            configure.Invoke(rabbitMQOptions);

            options.RegisterExtension(new RedisStreamsOptionsExtension(configure));

            return options;
        }
    }
}
