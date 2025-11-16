using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Shared dispatch pipeline for distributed event handlers (retry, multi-handler, logging).
    /// </summary>
    public class DistributedEventDispatcher
    {
        /// <summary>
        /// Root service provider used to create handler scopes.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// EventBus options including retry defaults and failure callback.
        /// </summary>
        private readonly EventBusOptions _options;

        /// <summary>
        /// Logger for deserialization, dispatch, and retry diagnostics.
        /// </summary>
        private readonly ILogger<DistributedEventDispatcher> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DistributedEventDispatcher"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
        /// <param name="options">Configured EventBus options.</param>
        /// <param name="logger">Optional logger; uses a null logger when omitted.</param>
        public DistributedEventDispatcher(
            IServiceProvider serviceProvider,
            IOptions<EventBusOptions> options,
            ILogger<DistributedEventDispatcher>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger ?? NullLogger<DistributedEventDispatcher>.Instance;
        }

        /// <summary>
        /// Deserializes a JSON payload and dispatches it to all registered
        /// <see cref="IDistributedEventHandler{TEvent}"/> instances, with retry support.
        /// </summary>
        /// <param name="eventType">The CLR type of the event to deserialize and handle.</param>
        /// <param name="payloadJson">The JSON payload representing the event.</param>
        /// <param name="maxRetry">Additional retry attempts after the first try.</param>
        /// <param name="retryIntervalSeconds">Delay in seconds between retry attempts.</param>
        /// <param name="cancellationToken">Token used to cancel dispatch or retry delays.</param>
        /// <returns>
        /// An <see cref="EventDispatchResult"/> indicating handled, no handler, or retry exhausted.
        /// </returns>
        public async Task<EventDispatchResult> DispatchAsync(
            Type eventType,
            string payloadJson,
            int maxRetry,
            int retryIntervalSeconds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(eventType);
            ArgumentException.ThrowIfNullOrEmpty(payloadJson);

            object? eventMessage;
            try
            {
                eventMessage = JsonSerializer.Deserialize(payloadJson, eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize event type {EventType}.", eventType.Name);
                _options.FailureCallback?.Invoke(eventType.Name, payloadJson);
                return EventDispatchResult.RetryExhausted;
            }

            if (eventMessage == null)
            {
                _logger.LogWarning("Deserialized event {EventType} was null.", eventType.Name);
                _options.FailureCallback?.Invoke(eventType.Name, payloadJson);
                return EventDispatchResult.RetryExhausted;
            }

            using var scope = _serviceProvider.CreateScope();
            var handlerServiceType = typeof(IDistributedEventHandler<>).MakeGenericType(eventType);
            var handlers = scope.ServiceProvider.GetServices(handlerServiceType).Cast<object>().ToList();

            if (handlers.Count == 0)
            {
                _logger.LogWarning("No distributed handlers registered for {EventType}.", eventType.Name);
                return EventDispatchResult.NoHandler;
            }

            var attempt = 0;
            var allowedAttempts = Math.Max(1, maxRetry + 1);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempt++;

                try
                {
                    foreach (var handler in handlers)
                    {
                        var method = handler.GetType().GetMethod(nameof(IEventHandler<IEvent>.HandleAsync), new[] { eventType })
                            ?? handler.GetType().GetMethod("HandleAsync");

                        if (method == null)
                            throw new InvalidOperationException($"HandleAsync not found on {handler.GetType().Name}.");

                        var task = (Task?)method.Invoke(handler, new[] { eventMessage });
                        if (task != null)
                            await task.ConfigureAwait(false);
                    }

                    _logger.LogDebug("Handled event {EventType} with {HandlerCount} handler(s).", eventType.Name, handlers.Count);
                    return EventDispatchResult.Handled;
                }
                catch (Exception ex) when (attempt < allowedAttempts)
                {
                    _logger.LogWarning(ex,
                        "Handler failed for {EventType}, attempt {Attempt}/{Allowed}. Retrying in {Seconds}s.",
                        eventType.Name, attempt, allowedAttempts, retryIntervalSeconds);

                    if (retryIntervalSeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(retryIntervalSeconds), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler retries exhausted for {EventType}.", eventType.Name);
                    _options.FailureCallback?.Invoke(eventType.Name, payloadJson);
                    return EventDispatchResult.RetryExhausted;
                }
            }
        }

        /// <summary>
        /// Parses retry settings from string header maps.
        /// </summary>
        /// <param name="headers">Optional message headers containing retry keys.</param>
        /// <param name="defaults">Fallback options when headers are missing or invalid.</param>
        /// <returns>
        /// A tuple of maximum retry count and retry interval in seconds.
        /// </returns>
        public static (int maxRetry, int retryIntervalSeconds) ParseRetryHeaders(
            IReadOnlyDictionary<string, string>? headers,
            EventBusOptions defaults)
        {
            var maxRetry = defaults.RetryCount;
            var interval = defaults.RetryInterval;

            if (headers != null)
            {
                if (headers.TryGetValue(EventMessageHeaders.Retry, out var retry)
                    && int.TryParse(retry, out var parsedRetry))
                    maxRetry = parsedRetry;

                if (headers.TryGetValue(EventMessageHeaders.RetryInterval, out var retryTime)
                    && int.TryParse(retryTime, out var parsedInterval))
                    interval = parsedInterval;
            }

            return (maxRetry, interval);
        }

        /// <summary>
        /// Parses retry settings from object header maps (for example, RabbitMQ).
        /// </summary>
        /// <param name="headers">Optional message headers whose values may be scalars or UTF-8 byte arrays.</param>
        /// <param name="defaults">Fallback options when headers are missing.</param>
        /// <returns>
        /// A tuple of maximum retry count and retry interval in seconds.
        /// </returns>
        public static (int maxRetry, int retryIntervalSeconds) ParseRetryHeaders(
            IReadOnlyDictionary<string, object>? headers,
            EventBusOptions defaults)
        {
            var maxRetry = defaults.RetryCount;
            var interval = defaults.RetryInterval;

            if (headers != null)
            {
                if (headers.TryGetValue(EventMessageHeaders.Retry, out var retry))
                    maxRetry = Convert.ToInt32(retry is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : retry);

                if (headers.TryGetValue(EventMessageHeaders.RetryInterval, out var retryTime))
                    interval = Convert.ToInt32(retryTime is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : retryTime);
            }

            return (maxRetry, interval);
        }
    }
}
