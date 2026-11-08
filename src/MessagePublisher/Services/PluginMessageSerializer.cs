using System.Reflection;
using MessagePublisher.Configuration;

namespace MessagePublisher.Services;

/// <summary>
/// Invokes a configured method on a package type (static or instance via <see cref="PluginServiceContainer"/>).
/// </summary>
public sealed class PluginMessageSerializer : IMessageSerializer
{
    private readonly SerializationPluginSettings _settings;
    private readonly PluginServiceContainer _container;
    private readonly Type _serializerType;
    private readonly MethodInfo _method;
    private readonly bool _passRuntimeType;
    private readonly object? _instance;

    public PluginMessageSerializer(
        SerializationPluginSettings settings,
        PluginServiceContainer container,
        TypeResolver typeResolver)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _container = container ?? throw new ArgumentNullException(nameof(container));
        ArgumentNullException.ThrowIfNull(typeResolver);

        if (string.IsNullOrWhiteSpace(settings.SerializerTypeFullName))
            throw new InvalidOperationException("Serializer type full name is required for plugin serialization.");
        if (string.IsNullOrWhiteSpace(settings.SerializerMethodName))
            throw new InvalidOperationException("Serializer method name is required for plugin serialization.");

        _serializerType = typeResolver.Resolve(settings.SerializerTypeFullName.Trim())
            ?? throw new InvalidOperationException(
                $"Serializer type '{settings.SerializerTypeFullName}' was not found in the loaded package.");

        _method = ResolveMethod(_serializerType, settings.SerializerMethodName.Trim(), settings.UseStaticMethod)
            ?? throw new InvalidOperationException(
                $"Method '{settings.SerializerMethodName}' on '{_serializerType.FullName}' could not be resolved.");

        _passRuntimeType = _method.GetParameters().Length >= 2;
        _instance = _settings.UseStaticMethod ? null : _container.GetRequiredService(_serializerType);
    }

    public string Serialize(object instance, Type runtimeType)
    {
        var args = _passRuntimeType
            ? new[] { instance, runtimeType }
            : new[] { instance };

        var result = _method.Invoke(_instance, args);
        return result switch
        {
            null => throw new InvalidOperationException("Serializer method returned null."),
            string s => s,
            _ => throw new InvalidOperationException(
                $"Serializer method must return a string; got {result.GetType().FullName}.")
        };
    }

    private static MethodInfo? ResolveMethod(Type serializerType, string methodName, bool isStatic)
    {
        const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var flags = isStatic ? staticFlags : instanceFlags;

        var candidates = serializerType
            .GetMethods(flags)
            .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
            .ToList();

        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderByDescending(ScoreMethod)
            .FirstOrDefault(m => m.ReturnType == typeof(string));
    }

    private static int ScoreMethod(MethodInfo method)
    {
        var ps = method.GetParameters();
        if (ps.Length == 2 && ps[0].ParameterType == typeof(object) && ps[1].ParameterType == typeof(Type))
            return 3;
        if (ps.Length == 1 && ps[0].ParameterType == typeof(object))
            return 2;
        if (ps.Length == 1)
            return 1;
        return 0;
    }
}
