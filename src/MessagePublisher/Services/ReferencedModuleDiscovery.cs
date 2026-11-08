using System.Reflection;


namespace MessagePublisher.Services;

/// <summary>
/// Discovers message types from the referenced <see cref="MessageTypes"/> project (local stand-in for package A).
/// </summary>
public static class ReferencedModuleDiscovery
{
    private static readonly Assembly MessageTypesAssembly = typeof(OrderPlacedMessage).Assembly;

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
            types = MessageTypesAssembly.GetTypes();
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

    private static Type? ResolveBaseType(string baseTypeFullName)
    {
        if (string.Equals(baseTypeFullName, nameof(BaseMessage), StringComparison.Ordinal)
            || baseTypeFullName == typeof(BaseMessage).FullName)
        {
            return typeof(BaseMessage);
        }

        return Type.GetType(baseTypeFullName, throwOnError: false, ignoreCase: false)
               ?? typeof(BaseMessage).Assembly.GetType(baseTypeFullName, throwOnError: false, ignoreCase: false);
    }

    private static bool HasUsableParameterlessConstructor(Type t) =>
        t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null) != null;
}
