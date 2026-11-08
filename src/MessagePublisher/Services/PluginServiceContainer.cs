using System.Collections.Concurrent;
using System.Reflection;
using MessagePublisher.Configuration;

namespace MessagePublisher.Services;

/// <summary>
/// Rudimentary constructor-injection container for types loaded from NuGet packages A/B.
/// </summary>
public sealed class PluginServiceContainer
{
    private readonly ConcurrentDictionary<string, Type> _implementationByServiceKey = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ServiceLifetime> _lifetimeByServiceKey = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, object> _singletons = new(StringComparer.Ordinal);
    private readonly TypeResolver _types;
    private readonly object _singletonLock = new();

    public PluginServiceContainer(TypeResolver types, IEnumerable<ServiceRegistrationSettings> registrations)
    {
        _types = types ?? throw new ArgumentNullException(nameof(types));

        foreach (var reg in registrations)
        {
            if (string.IsNullOrWhiteSpace(reg.ImplementationTypeFullName))
                continue;

            var key = NormalizeServiceKey(reg.ServiceTypeFullName, reg.ImplementationTypeFullName);
            _implementationByServiceKey[key] = RequireType(reg.ImplementationTypeFullName);
            _lifetimeByServiceKey[key] = reg.Lifetime;
        }
    }

    public object GetRequiredService(Type serviceType)
    {
        var key = serviceType.FullName ?? serviceType.Name;
        if (_implementationByServiceKey.TryGetValue(key, out var implementation))
            return ResolveRegistered(key, implementation);

        return CreateInstance(serviceType, trackSingleton: true);
    }

    public object CreateInstance(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        return CreateInstance(implementationType, trackSingleton: false);
    }

    private object ResolveRegistered(string serviceKey, Type implementationType)
    {
        var lifetime = _lifetimeByServiceKey.GetValueOrDefault(serviceKey, ServiceLifetime.Singleton);
        if (lifetime == ServiceLifetime.Transient)
            return CreateInstance(implementationType, trackSingleton: false);

        lock (_singletonLock)
        {
            if (_singletons.TryGetValue(serviceKey, out var existing))
                return existing;

            var created = CreateInstance(implementationType, trackSingleton: false);
            _singletons[serviceKey] = created;
            return created;
        }
    }

    private object CreateInstance(Type implementationType, bool trackSingleton)
    {
        if (implementationType.IsAbstract || implementationType.IsInterface)
            throw new InvalidOperationException($"Type '{implementationType.FullName}' cannot be instantiated.");

        var ctor = SelectConstructor(implementationType)
            ?? throw new InvalidOperationException(
                $"No public constructor on '{implementationType.FullName}' could be satisfied.");

        var args = ctor.GetParameters()
            .Select(p => GetRequiredService(p.ParameterType))
            .ToArray();

        return ctor.Invoke(args);
    }

    private static ConstructorInfo? SelectConstructor(Type implementationType) =>
        implementationType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

    private Type RequireType(string typeFullName)
    {
        var type = _types.Resolve(typeFullName)
            ?? throw new InvalidOperationException($"Type '{typeFullName}' was not found in loaded assemblies.");
        return type;
    }

    private static string NormalizeServiceKey(string? serviceTypeFullName, string implementationTypeFullName) =>
        string.IsNullOrWhiteSpace(serviceTypeFullName) ? implementationTypeFullName : serviceTypeFullName.Trim();
}

/// <summary>
/// Resolves CLR types from assemblies loaded in a package context.
/// </summary>
public sealed class TypeResolver
{
    private readonly IReadOnlyList<Assembly> _assemblies;

    public TypeResolver(IReadOnlyList<Assembly> assemblies)
    {
        _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
    }

    public Type? Resolve(string typeFullName)
    {
        if (string.IsNullOrWhiteSpace(typeFullName))
            return null;

        foreach (var asm in _assemblies)
        {
            var t = asm.GetType(typeFullName.Trim(), throwOnError: false, ignoreCase: false);
            if (t != null)
                return t;
        }

        foreach (var asm in _assemblies)
        {
            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.FullName == typeFullName || t.Name == typeFullName)
                        return t;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var t in ex.Types.Where(x => x != null).Cast<Type>())
                {
                    if (t.FullName == typeFullName || t.Name == typeFullName)
                        return t;
                }
            }
        }

        return Type.GetType(typeFullName, throwOnError: false, ignoreCase: false);
    }
}
