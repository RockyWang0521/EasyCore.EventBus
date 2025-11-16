using Microsoft.Extensions.DependencyModel;
using System.Reflection;

namespace EasyCore.EventBus.Event
{
    /// <summary>
    /// Discovers event and handler types from loaded application assemblies.
    /// </summary>
    public static class EventTypeScanner
    {
        /// <summary>
        /// Returns candidate application assemblies without loading every DLL from the base directory.
        /// </summary>
        /// <returns>A read-only list of assemblies that may contain events or handlers.</returns>
        public static IReadOnlyList<Assembly> GetApplicationAssemblies()
        {
            var assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(Assembly? assembly)
            {
                if (assembly == null)
                    return;

                var name = assembly.GetName().Name;
                if (string.IsNullOrEmpty(name) || IsSystemAssembly(name))
                    return;

                assemblies.TryAdd(name, assembly);
            }

            TryAdd(Assembly.GetEntryAssembly());
            TryAdd(Assembly.GetCallingAssembly());
            TryAdd(Assembly.GetExecutingAssembly());

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                TryAdd(assembly);

            try
            {
                var dependencyContext = DependencyContext.Default;
                if (dependencyContext != null)
                {
                    foreach (var library in dependencyContext.RuntimeLibraries)
                    {
                        if (IsSystemAssembly(library.Name))
                            continue;

                        try
                        {
                            TryAdd(Assembly.Load(new AssemblyName(library.Name)));
                        }
                        catch
                        {
                            // Ignore libraries that cannot be loaded in the current context.
                        }
                    }
                }
            }
            catch
            {
                // DependencyContext may be unavailable in some hosts.
            }

            return assemblies.Values.ToList();
        }

        /// <summary>
        /// Finds event types that have at least one <see cref="IDistributedEventHandler{TEvent}"/> implementation.
        /// </summary>
        /// <returns>A read-only list of distributed event types keyed by type name during discovery.</returns>
        public static IReadOnlyList<Type> GetDistributedEventTypes()
        {
            var eventTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
            var handlerOpenType = typeof(IDistributedEventHandler<>);

            foreach (var assembly in GetApplicationAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }

                foreach (var type in types)
                {
                    if (type is not { IsClass: true, IsAbstract: false })
                        continue;

                    foreach (var iface in type.GetInterfaces())
                    {
                        if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != handlerOpenType)
                            continue;

                        var eventType = iface.GetGenericArguments()[0];
                        if (typeof(IEvent).IsAssignableFrom(eventType))
                            eventTypes[eventType.Name] = eventType;
                    }
                }
            }

            return eventTypes.Values.ToList();
        }

        /// <summary>
        /// Finds all concrete handler types for local and distributed events.
        /// </summary>
        /// <returns>
        /// Two lists of service/implementation pairs for local and distributed handler registrations.
        /// </returns>
        public static (IReadOnlyList<(Type Service, Type Implementation)> Local, IReadOnlyList<(Type Service, Type Implementation)> Distributed)
            GetHandlerRegistrations()
        {
            var local = new List<(Type, Type)>();
            var distributed = new List<(Type, Type)>();
            var localOpen = typeof(ILocalEventHandler<>);
            var distributedOpen = typeof(IDistributedEventHandler<>);

            foreach (var assembly in GetApplicationAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }

                foreach (var type in types)
                {
                    if (type is not { IsClass: true, IsAbstract: false })
                        continue;

                    foreach (var iface in type.GetInterfaces())
                    {
                        if (!iface.IsGenericType)
                            continue;

                        var definition = iface.GetGenericTypeDefinition();
                        if (definition == localOpen)
                            local.Add((iface, type));
                        else if (definition == distributedOpen)
                            distributed.Add((iface, type));
                    }
                }
            }

            return (local, distributed);
        }

        /// <summary>
        /// Determines whether an assembly name belongs to a system or framework assembly.
        /// </summary>
        /// <param name="name">The assembly simple name to evaluate.</param>
        /// <returns><c>true</c> if the assembly should be skipped during scanning; otherwise, <c>false</c>.</returns>
        private static bool IsSystemAssembly(string name) =>
            name.StartsWith("System", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("WindowsBase", StringComparison.OrdinalIgnoreCase)
            || name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase);
    }
}
