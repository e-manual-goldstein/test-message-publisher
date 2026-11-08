using System.Reflection;
using MessagePublisher.Configuration;

namespace MessagePublisher.Services;

public static class MessageSerializerFactory
{
    public static (IMessageSerializer Serializer, string? StatusNote) Create(
        SerializationPluginSettings plugin,
        DependencyInjectionSettings dependencyInjection,
        IReadOnlyList<Assembly> loadedAssemblies)
    {
        if (string.IsNullOrWhiteSpace(plugin.SerializerTypeFullName)
            || string.IsNullOrWhiteSpace(plugin.SerializerMethodName))
        {
            return (new JsonMessageSerializer(), "Using built-in JSON serializer (no serialization plugin configured).");
        }

        var typeResolver = new TypeResolver(loadedAssemblies);
        var container = new PluginServiceContainer(typeResolver, dependencyInjection.Services);

        var serializer = new PluginMessageSerializer(plugin, container, typeResolver);
        var note =
            $"Using plugin serializer {plugin.SerializerTypeFullName}.{plugin.SerializerMethodName}" +
            (plugin.UseStaticMethod ? " (static)" : " (instance)");
        return (serializer, note);
    }
}
