using EasyCore.EventBus.Distributed;
using EasyCore.EventBus.Event;
using EasyCore.EventBus.HostedService;
using EasyCore.EventBus.Local;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EasyCore.EventBus
{
    public static class EasyCoreEventExtend
    {
        public static void EasyCoreEventBus(this IServiceCollection service, Action<EventBusOptions>? action = null)
        {
            service.AddSingleton<ILocalEventBus, LocalEventBus>();

            if (action != null)
            {
                service.AddSingleton<IDistributedEventBus, DistributedEventBus>();

                service.AddOptions();

                var options = new EventBusOptions();

                action(options);

                if (options.Extensions != null)
                {
                    foreach (var serviceExtension in options.Extensions!)
                        serviceExtension.AddServices(service);

                    service.AddHostedService<EventBusHostedService>();
                }

                service.Configure(action);
            }

            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string[] dllFiles = Directory.GetFiles(rootDirectory, "*.dll", SearchOption.TopDirectoryOnly).Where(path =>
            {
                string fileName = Path.GetFileName(path);
                return !(fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) || fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase));
            }).ToArray();

            var eventtype = typeof(IEvent);

            List<Type> eventHandlerLocalTypes = new List<Type>();

            List<Type> eventHandlerDistributedTypes = new List<Type>();

            foreach (var dll in dllFiles)
            {
                Assembly assembly = Assembly.LoadFrom(dll);

                var events = assembly.GetTypes()
                                             .Where(type => eventtype.IsAssignableFrom(type))
                                             .Where(type => !type.IsInterface)
                                             .Where(type => !type.IsAbstract);

                if (events.Count() <= 0) continue;

                foreach (var eventType in events)
                {
                    var handlerLocalType = typeof(ILocalEventHandler<>).MakeGenericType(eventType);

                    var handlerDistributedType = typeof(IDistributedEventHandler<>).MakeGenericType(eventType);

                    if (!eventHandlerLocalTypes.Contains(handlerLocalType)) eventHandlerLocalTypes.Add(handlerLocalType);

                    if (!eventHandlerDistributedTypes.Contains(handlerDistributedType)) eventHandlerDistributedTypes.Add(handlerDistributedType);
                }

            }

            if (eventHandlerLocalTypes.Count <= 0 && eventHandlerDistributedTypes.Count <= 0) return;

            foreach (var dll in dllFiles)
            {
                Assembly assembly = Assembly.LoadFrom(dll);

                foreach (var handlerType in eventHandlerLocalTypes)
                {
                    var handlers = assembly.GetTypes().Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                    foreach (var handler in handlers)
                        service.AddTransient(handlerType, handler);
                }

                foreach (var handlerType in eventHandlerDistributedTypes)
                {
                    var handlers = assembly.GetTypes().Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                    foreach (var handler in handlers)
                        service.AddTransient(handlerType, handler);
                }
            }
        }
    }
}


