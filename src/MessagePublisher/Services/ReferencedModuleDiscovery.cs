using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MessagePublisher.Services;

/// <summary>
/// Discovers message types from the referenced <see cref="MessageTypes"/> project (local stand-in for package A).
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

        Type[] types;
        try
        {
            types = GetAllTypesFromReferencedAssemblies().ToArray();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }

        foreach (var t in types)
        {
            if (t is null || !t.IsClass || t.IsAbstract || t.IsNotPublic)
                continue;
            if (!baseType.IsAssignableFrom(t))
                continue;
            if (!HasUsableParameterlessConstructor(t))
                continue;

            result.Add(new DiscoveredMessageType(t, t.Name));
        }

        return result.OrderBy(r => r.ShortName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<Type> GetAllTypesFromReferencedAssemblies()
    {
        //@CURSOR Implement this method. Get the Types from all assemblies currently referenced by the application
        
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
