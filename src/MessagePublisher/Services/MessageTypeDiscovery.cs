using System.Linq;
using System.Reflection;

namespace MessagePublisher.Services;

public sealed record DiscoveredMessageType(Type ClrType, string ShortName);

/// <summary>
/// Loads assemblies from package B (base type) and package A (message types) and discovers concrete message types.
/// </summary>
public static class MessageTypeDiscovery
{
    public static IReadOnlyList<DiscoveredMessageType> Discover(
        string messageTypesLibDirectory,
        string baseTypeLibDirectory,
        string baseTypeFullName,
        out MessagePackageLoadContext loadContext,
        out IReadOnlyList<Assembly> loadedAssemblies)
    {
        if (string.IsNullOrWhiteSpace(baseTypeFullName))
            throw new ArgumentException("Base type full name is required.", nameof(baseTypeFullName));

        loadContext = new MessagePackageLoadContext([baseTypeLibDirectory, messageTypesLibDirectory]);

        var basePaths = PackageAssemblyResolver.GetAssemblyPaths(baseTypeLibDirectory);
        var messagePaths = PackageAssemblyResolver.GetAssemblyPaths(messageTypesLibDirectory);

        var loadedBase = LoadAssemblies(loadContext, basePaths);
        var loadedMessages = LoadAssemblies(loadContext, messagePaths);

        var baseType = ResolveBaseType(loadedBase, baseTypeFullName)
                       ?? ResolveBaseType(loadedBase.Concat(loadedMessages).ToList(), baseTypeFullName)
                       ?? throw new InvalidOperationException(
                           $"Could not resolve base type '{baseTypeFullName}'. Ensure package B is loaded and the name matches.");

        var messageAssemblies = new HashSet<Assembly>(loadedMessages);
        var result = new List<DiscoveredMessageType>();

        foreach (var asm in messageAssemblies)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
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
        }

        loadedAssemblies = loadedBase
            .Concat(loadedMessages)
            .GroupBy(a => a.FullName)
            .Select(g => g.First())
            .ToList();

        return result.OrderBy(r => r.ShortName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<Assembly> LoadAssemblies(MessagePackageLoadContext ctx, IReadOnlyList<string> paths)
    {
        var loaded = new List<Assembly>();
        foreach (var path in paths)
        {
            try
            {
                loaded.Add(ctx.LoadFromAssemblyPath(path));
            }
            catch (BadImageFormatException)
            {
            }
            catch (FileLoadException)
            {
            }
        }

        return loaded;
    }

    private static Type? ResolveBaseType(IReadOnlyList<Assembly> assemblies, string baseTypeFullName)
    {
        foreach (var asm in assemblies)
        {
            var t = asm.GetType(baseTypeFullName, throwOnError: false, ignoreCase: false);
            if (t != null)
                return t;
        }

        foreach (var asm in assemblies)
        {
            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.FullName == baseTypeFullName || t.Name == baseTypeFullName)
                        return t;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var t in ex.Types.Where(x => x != null).Cast<Type>())
                {
                    if (t.FullName == baseTypeFullName || t.Name == baseTypeFullName)
                        return t;
                }
            }
        }

        return Type.GetType(baseTypeFullName, throwOnError: false, ignoreCase: false);
    }

    private static bool HasUsableParameterlessConstructor(Type t)
    {
        return t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                Type.EmptyTypes,
                modifiers: null)
            != null;
    }
}
