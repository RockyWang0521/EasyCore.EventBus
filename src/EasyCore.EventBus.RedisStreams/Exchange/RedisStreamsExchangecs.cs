using EasyCore.EventBus.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Reflection;
using System.Text.Json;

namespace EasyCore.EventBus.RedisStreams.Exchange
{
    public class RedisStreamsExchangecs : IRedisStreamsExchangecs, IDisposable
    {
        private readonly IConnectionChannel _connectionChannel;
        private readonly IServiceProvider _serviceProvider;
        private readonly EventBusOptions _eventBusoptions;
        private IDatabase? _database;
        private IConnectionMultiplexer? _connectionMultiplexer;
        private Dictionary<string, Type>? _streams;
        private string? _appName;
        private StreamPosition[]? _streamPositions;
        private string? _consumerGroup;

        public RedisStreamsExchangecs(
            IConnectionChannel connectionChannel,
            IOptions<EventBusOptions> eventBusoptions,
            IServiceProvider serviceProvider)
        {
            _connectionChannel = connectionChannel;
            _eventBusoptions = eventBusoptions.Value;
            _serviceProvider = serviceProvider;
            _appName = Assembly.GetEntryAssembly()!.GetName().Name;
            _consumerGroup = $"{_appName}.RedisGroup";
        }

        public async Task<bool> PublishAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent => await SendAsync(eventMessage);

        public bool Publish<TEvent>(TEvent eventMessage) where TEvent : IEvent => Send(eventMessage);

        private async Task<bool> SendAsync<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            try
            {
                if (_database == null) _database = _connectionChannel.GetDatabase(_connectionMultiplexer!);

                var streamKey = eventMessage.GetType().Name;

                var headers = new RedisHeader
                {
                    RetryCount = _eventBusoptions.RetryCount,

                    RetryInterval = _eventBusoptions.RetryInterval,
                };

                var message = new NameValueEntry[]
                {
                  new NameValueEntry("RedisHeader", JsonSerializer.Serialize(headers)),

                  new NameValueEntry(eventMessage.GetType().Name, JsonSerializer.Serialize(eventMessage))
                };

                var streamId = await _database.StreamAddAsync(streamKey, message);

                if (!string.IsNullOrEmpty(streamId))
                    return true;
                else
                    return false;
            }
            catch
            {
                throw;
            }
        }

        private bool Send<TEvent>(TEvent eventMessage) where TEvent : IEvent
        {
            try
            {
                if (_database == null) _database = _connectionChannel.GetDatabase(_connectionMultiplexer!);

                var streamKey = eventMessage.GetType().Name;

                var headers = new RedisHeader
                {
                    RetryCount = _eventBusoptions.RetryCount,

                    RetryInterval = _eventBusoptions.RetryInterval,
                };

                var message = new NameValueEntry[]
                {
                  new NameValueEntry("headers", JsonSerializer.Serialize(headers)),

                  new NameValueEntry("message", JsonSerializer.Serialize(eventMessage))
                };

                var streamId = _database.StreamAdd(streamKey, message);

                if (!string.IsNullOrEmpty(streamId))
                    return true;
                else
                    return false;
            }
            catch
            {
                throw;
            }
        }

        public void Connect()
        {
            if (_connectionMultiplexer == null) _connectionMultiplexer = _connectionChannel.GetConnection();

            if (_database == null) _database = _connectionChannel.GetDatabase(_connectionMultiplexer!);
        }

        public async void Subscribe()
        {
            if (_database == null) _database = _connectionChannel.GetDatabase(_connectionMultiplexer!);

            _streams = GetStreams();

            var streams = new List<string>(_streams.Keys).ToArray();

            _streamPositions = await GetStreamPositions(new List<string>(streams), _database);

            foreach (var stream in streams) await CreateConsumerGroupAsync(_database, stream, _consumerGroup!);

            _ = Task.Run(async () =>
                  {
                      while (true)
                      {
                          var streamReadGroups = _database.StreamReadGroupAsync(_streamPositions, _consumerGroup!, _consumerGroup!, 1);

                          var readSet = await Task.WhenAll(streamReadGroups).ConfigureAwait(false);

                          var message = readSet.SelectMany(set => set);

                          foreach (var msg in message)
                          {
                              var messageId = msg.Entries[0].Id;

                              var typeName = msg.Entries[0].Values[1].Name;

                              var header = JsonSerializer.Deserialize<RedisHeader>(msg.Entries[0].Values[0].Value!);

                              var eventMessage = msg.Entries[0].Values[1].Value!;

                              await Received(header, typeName, eventMessage, messageId, _database);
                          }
                      }
                  });
        }

        private async Task Received(RedisHeader? header, string? typeName, string? eventMessage, RedisValue messageId, IDatabase database)
        {
            try
            {
                if (header == null) throw new ArgumentException("Header cannot be null.");

                if (string.IsNullOrEmpty(typeName)) throw new ArgumentException("Type name cannot be null or empty.");

                if (string.IsNullOrEmpty(eventMessage)) throw new ArgumentException("Event message cannot be null or empty.");

                if (messageId == RedisValue.Null) throw new ArgumentException("Message id cannot be null.");

                Type? eventType = null;

                if (_streams != null && _streams?.Count > 0 && !string.IsNullOrEmpty(typeName))
                {
                    eventType = _streams[typeName!];
                }

                if (eventType == null) return;

                var message = JsonSerializer.Deserialize(eventMessage, eventType);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var handler = scope.ServiceProvider.GetService(typeof(IDistributedEventHandler<>).MakeGenericType(eventType));

                    if (handler != null)
                    {

#pragma warning disable CS8600
#pragma warning disable CS8602

                        var maxRetry = 0;

                        do
                        {
                            try
                            {
                                maxRetry++;

                                await (Task)handler.GetType().GetMethod("HandleAsync", new[] { message.GetType() }).Invoke(handler, new object[] { message! });

                                await database.StreamAcknowledgeAsync(typeName, _consumerGroup!, messageId);

                                break;
                            }
                            catch
                            {
                                if (maxRetry > header.RetryCount) throw;

                                await Task.Delay((int)header.RetryInterval! * 1000);
                            }
                        }
                        while (true);
                    }
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch
            {
                _eventBusoptions.FailureCallback?.Invoke(typeName!, eventMessage);

                await database.StreamAcknowledgeAsync(typeName, _consumerGroup!, messageId);
            }
        }

        private Dictionary<string, Type> GetStreams()
        {
            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string[] dllFiles = Directory.GetFiles(rootDirectory, "*.dll");

            var streams = new Dictionary<string, Type>();

            var eventType = typeof(IEvent);

            var handlerType = typeof(IDistributedEventHandler<>);

            foreach (var dll in dllFiles)
            {
                Assembly assembly = Assembly.LoadFrom(dll);

                var handlers = assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract)
                    .Where(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType));

                foreach (var handler in handlers)
                {
                    var eventInterfaces = handler.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType).ToList();

                    foreach (var eventInterface in eventInterfaces)
                    {
                        var eventTypeArgument = eventInterface.GetGenericArguments()[0];

                        if (eventType.IsAssignableFrom(eventTypeArgument)) streams.Add(eventTypeArgument.Name, eventTypeArgument);
                    }
                }
            }

            return streams;
        }

        private async Task<StreamPosition[]> GetStreamPositions(List<string> streams, IDatabase database)
        {
            var streamPositions = streams.Select(stream => new StreamPosition(stream, StreamPosition.NewMessages));

            foreach (var stream in streams)
            {
                await CreateStreamAsync(database, stream);

                await CreateConsumerGroupAsync(database, stream, _consumerGroup!);
            }

            return streamPositions.ToArray();
        }

        private async Task CreateConsumerGroupAsync(IDatabase database, RedisKey streamKey, string consumerGroup)
        {
            try
            {
                var groupInfo = await database.StreamGroupInfoAsync(streamKey);

                if (groupInfo.Any(g => g.Name == consumerGroup)) return;

                await database.StreamCreateConsumerGroupAsync(streamKey, consumerGroup, StreamPosition.NewMessages).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("BUSYGROUP Consumer Group name already exists"))
                {
                    Console.WriteLine($"Consumer group '{consumerGroup}' already exists.");
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task CreateStreamAsync(IDatabase database, RedisKey streamKey)
        {
            try
            {
                var streamExists = await database.KeyExistsAsync(streamKey);

                if (streamExists) return;

                await database.StreamAddAsync(streamKey, "status", "created");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            _connectionMultiplexer.CloseAsync();

            _connectionMultiplexer.DisposeAsync();
        }
    }
}
