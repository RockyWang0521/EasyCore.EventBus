using EasyCore.EventBus.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyCore.EventBus.Local
{
    /// <summary>
    /// In-process event bus that resolves and invokes local handlers from DI.
    /// </summary>
    public class LocalEventBus : ILocalEventBus
    {
        /// <summary>
        /// Root service provider used to create handler scopes.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Logger for local publish and handler failures.
        /// </summary>
        private readonly ILogger<LocalEventBus> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="LocalEventBus"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
        /// <param name="logger">Optional logger; uses a null logger when omitted.</param>
        public LocalEventBus(IServiceProvider serviceProvider, ILogger<LocalEventBus>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger ?? NullLogger<LocalEventBus>.Instance;
        }

        /// <summary>
        /// Publishes an event asynchronously to all registered local handlers.
        /// </summary>
        /// <typeparam name="TEvent">The event type, which must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="eventMessage">The event instance to publish.</param>
        /// <returns>A task that completes when all handlers have finished processing.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventMessage"/> is <c>null</c>.</exception>
        public async Task PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(eventMessage);

            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<ILocalEventHandler<TEvent>>().ToList();

            if (handlers.Count == 0)
            {
                _logger.LogDebug("No local handlers for {EventType}.", typeof(TEvent).Name);
                return;
            }

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(eventMessage).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Local handler {Handler} failed for {EventType}.",
                        handler.GetType().Name, typeof(TEvent).Name);
                    throw;
                }
            }
        }
    }
}
