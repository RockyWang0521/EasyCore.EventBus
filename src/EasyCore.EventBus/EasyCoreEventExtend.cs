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

            var eventType = typeof(IEvent);

            HashSet<Type> eventHandlerLocalTypes = new HashSet<Type>();

            HashSet<Type> eventHandlerDistributedTypes = new HashSet<Type>();

            List<Assembly> assemblies = new List<Assembly>();

            foreach (var dll in dllFiles)
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(dll));
                }
                catch (Exception)
                {
                    continue;
                }
            }

            foreach (var assembly in assemblies)
            {
                var events = GetLoadableTypes(assembly)
                             .Where(type => eventType.IsAssignableFrom(type))
                             .Where(type => !type.IsInterface)
                             .Where(type => !type.IsAbstract);

                if (!events.Any()) continue;

                foreach (var eventItem in events)
                {
                    var handlerLocalType = typeof(ILocalEventHandler<>).MakeGenericType(eventItem);

                    var handlerDistributedType = typeof(IDistributedEventHandler<>).MakeGenericType(eventItem);

                    eventHandlerLocalTypes.Add(handlerLocalType);

                    eventHandlerDistributedTypes.Add(handlerDistributedType);
                }

            }

            if (eventHandlerLocalTypes.Count <= 0 && eventHandlerDistributedTypes.Count <= 0) return;

            foreach (var assembly in assemblies)
            {
                foreach (var handlerType in eventHandlerLocalTypes)
                {
                    var handlers = GetLoadableTypes(assembly).Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                    foreach (var handler in handlers)
                        service.AddTransient(handlerType, handler);
                }

                foreach (var handlerType in eventHandlerDistributedTypes)
                {
                    var handlers = GetLoadableTypes(assembly).Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                    foreach (var handler in handlers)
                        service.AddTransient(handlerType, handler);
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null)!;
            }
        }
    }
}

