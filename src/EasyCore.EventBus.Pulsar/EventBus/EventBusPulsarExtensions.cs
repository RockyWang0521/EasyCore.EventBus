namespace EasyCore.EventBus.Pulsar
{
    public static class EventBusPulsarExtensions
    {
        public static EventBusOptions Pulsar(this EventBusOptions options, string serviceUrl)
        {
            if (string.IsNullOrEmpty(serviceUrl))
                throw new ArgumentException("Service URL is required.", nameof(serviceUrl));

            return options.Pulsar(opt => opt.ServiceUrl = serviceUrl);
        }

        public static EventBusOptions Pulsar(this EventBusOptions options, Action<PulsarOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            options.RegisterExtension(new PulsarOptionsExtension(configure));
            return options;
        }
    }
}
