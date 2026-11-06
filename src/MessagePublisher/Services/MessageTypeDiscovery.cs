using System.Linq;
using System.Reflection;

namespace MessagePublisher.Services;

public sealed record DiscoveredMessageType(Type ClrType, string DisplayName);

/// <summary>
/// Loads assemblies from a package lib folder and discovers concrete message types assignable to a base type.
/// </summary>
public static class MessageTypeDiscovery
{
    public static IReadOnlyList<DiscoveredMessageType> Discover(
        string libDirectory,
        string baseTypeFullName,
        out MessagePackageLoadContext loadContext)
    {
        if (string.IsNullOrWhiteSpace(baseTypeFullName))
            throw new ArgumentException("Base type full name is required.", nameof(baseTypeFullName));

        loadContext = new MessagePackageLoadContext(libDirectory);
        var paths = PackageAssemblyResolver.GetAssemblyPaths(libDirectory);

        var loaded = new List<Assembly>();
        foreach (var path in paths)
        {
            try
            {
                loaded.Add(loadContext.LoadFromAssemblyPath(path));
            }
            catch (BadImageFormatException)
            {
                // Native/managed mix or wrong architecture — skip
            }
            catch (FileLoadException)
            {
                // Skip unloadable
            }
        }

        var baseType = ResolveBaseType(loaded, baseTypeFullName)
            ?? throw new InvalidOperationException(
                $"Could not resolve base type '{baseTypeFullName}' in any loaded assembly. Check spelling and that the base type is in this package.");

        var result = new List<DiscoveredMessageType>();
        foreach (var asm in loaded)
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

                result.Add(new DiscoveredMessageType(t, t.FullName ?? t.Name));
            }
        }

        return result.OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static Type? ResolveBaseType(IReadOnlyList<Assembly> assemblies, string baseTypeFullName)
    {
        // Allow simple full name; if comma present, try Type.GetType style.
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
