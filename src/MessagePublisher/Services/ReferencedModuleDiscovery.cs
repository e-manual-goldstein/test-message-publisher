using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MessagePublisher.Services;

/// <summary>
/// Discovers concrete message types by scanning types in the executing assembly's reference graph.
/// </summary>
public static class ReferencedModuleDiscovery
{
    public static IReadOnlyList<DiscoveredMessageType> Discover(string baseTypeFullName)
    {
        if (string.IsNullOrWhiteSpace(baseTypeFullName))
            throw new ArgumentException("Base type full name is required.", nameof(baseTypeFullName));

        var baseType = ResolveBaseType(baseTypeFullName.Trim())
            ?? throw new InvalidOperationException(
                $"Could not resolve base type '{baseTypeFullName}' from referenced modules.");

        var result = new List<DiscoveredMessageType>();

        foreach (var t in GetAllTypesFromReferencedAssemblies())
        {
            if (t == null || !t.IsClass || t.IsAbstract || t.IsNotPublic)
                continue;
            if (!baseType.IsAssignableFrom(t))
                continue;
            if (!HasUsableParameterlessConstructor(t))
                continue;

            result.Add(new DiscoveredMessageType(t, t.Name));
        }

        return result.OrderBy(r => r.ShortName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Walks transitive <see cref="Assembly.GetReferencedAssemblies"/> from the host and returns all loadable types.
    /// </summary>
    private static IEnumerable<Type> GetAllTypesFromReferencedAssemblies()
    {
        foreach (var assembly in LoadReferencedAssemblyClosure(Assembly.GetExecutingAssembly()))
        {
            foreach (var type in GetLoadableTypes(assembly))
                yield return type;
        }
    }

    private static IReadOnlyList<Assembly> LoadReferencedAssemblyClosure(Assembly root)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var loaded = new List<Assembly>();
        var queue = new Queue<Assembly>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            var fullName = assembly.FullName;
            if (fullName == null || !seen.Add(fullName))
                continue;

            loaded.Add(assembly);

            AssemblyName[] references;
            try
            {
                references = assembly.GetReferencedAssemblies();
            }
            catch
            {
                continue;
            }

            foreach (var reference in references)
            {
                if (reference.FullName != null && seen.Contains(reference.FullName))
                    continue;

                try
                {
                    queue.Enqueue(Assembly.Load(reference));
                }
                catch
                {
                    // Skip assemblies that cannot be loaded in this AppDomain.
                }
            }
        }

        return loaded;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).Cast<Type>();
        }
        catch
        {
            return Enumerable.Empty<Type>();
        }
    }

    private static Type? ResolveBaseType(string baseTypeFullName)
    {
        if (string.Equals(baseTypeFullName, nameof(Event), StringComparison.Ordinal)
            || baseTypeFullName == typeof(Event).FullName)
        {
            return typeof(Event);
        }

        return Type.GetType(baseTypeFullName, throwOnError: false, ignoreCase: false)
               ?? typeof(Event).Assembly.GetType(baseTypeFullName, throwOnError: false, ignoreCase: false);
    }

    private static bool HasUsableParameterlessConstructor(Type t) =>
        t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null) != null;
}
