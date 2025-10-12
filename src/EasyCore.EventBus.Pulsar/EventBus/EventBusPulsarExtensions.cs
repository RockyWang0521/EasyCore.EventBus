namespace EasyCore.EventBus.Pulsar
{
    public static class EventBusPulsarExtensions
    {
        public static EventBusOptions Pulsar(this EventBusOptions options, string ServiceUrl)
        {
            if (string.IsNullOrEmpty(ServiceUrl)) throw new ArgumentException(nameof(ServiceUrl));

            var configure = new Action<PulsarOptions>(options =>
            {
                options.ServiceUrl = ServiceUrl;
            });

            options.RegisterExtension(new PulsarOptionsExtension(configure));

            return options;
        }

        public static EventBusOptions Pulsar(this EventBusOptions options, Action<PulsarOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var pulsarOptions = new PulsarOptions();

            configure.Invoke(pulsarOptions);

            options.RegisterExtension(new PulsarOptionsExtension(configure));

            return options;
        }
    }
}
