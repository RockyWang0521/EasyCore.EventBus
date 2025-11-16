using EasyCore.EventBus.Event;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyCore.EventBus.HostedService
{
    /// <summary>
    /// Starts and stops the distributed event bus transport as a hosted service.
    /// </summary>
    public class EventBusHostedService : IHostedService, IAsyncDisposable
    {
        /// <summary>
        /// The message queue client used to connect and subscribe.
        /// </summary>
        private readonly IEventMessageQueueClient _messageQueueClient;

        /// <summary>
        /// Logger for hosted service lifecycle events.
        /// </summary>
        private readonly ILogger<EventBusHostedService> _logger;

        /// <summary>
        /// Linked cancellation source created when the service starts.
        /// </summary>
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Indicates whether <see cref="StopAsync"/> has already completed.
        /// </summary>
        private bool _stopped;

        /// <summary>
        /// Initializes a new instance of <see cref="EventBusHostedService"/>.
        /// </summary>
        /// <param name="messageQueueClient">The transport client to connect and subscribe.</param>
        /// <param name="logger">Optional logger; uses a null logger when omitted.</param>
        public EventBusHostedService(
            IEventMessageQueueClient messageQueueClient,
            ILogger<EventBusHostedService>? logger = null)
        {
            _messageQueueClient = messageQueueClient;
            _logger = logger ?? NullLogger<EventBusHostedService>.Instance;
        }

        /// <summary>
        /// Connects to the message queue and begins consuming events.
        /// </summary>
        /// <param name="cancellationToken">Token that signals host shutdown during startup.</param>
        /// <returns>A task that completes when connect and subscribe have finished.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _logger.LogInformation("Starting EventBus hosted service.");

            await _messageQueueClient.ConnectAsync(_cts.Token).ConfigureAwait(false);
            await _messageQueueClient.SubscribeAsync(_cts.Token).ConfigureAwait(false);

            _logger.LogInformation("EventBus hosted service started.");
        }

        /// <summary>
        /// Cancels subscriptions and disposes the message queue client.
        /// </summary>
        /// <param name="cancellationToken">Token that signals a forced stop deadline.</param>
        /// <returns>A task that completes when the transport has been disposed.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_stopped)
                return;

            _stopped = true;
            _logger.LogInformation("Stopping EventBus hosted service.");
            _cts?.Cancel();
            await _messageQueueClient.DisposeAsync().ConfigureAwait(false);
            _logger.LogInformation("EventBus hosted service stopped.");
        }

        /// <summary>
        /// Ensures the service is stopped and releases the cancellation token source.
        /// </summary>
        /// <returns>A value task that completes when disposal is finished.</returns>
        public async ValueTask DisposeAsync()
        {
            if (!_stopped)
                await StopAsync(CancellationToken.None).ConfigureAwait(false);

            _cts?.Dispose();
            _cts = null;
        }
    }
}
