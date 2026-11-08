namespace MessagePublisher.Configuration;

/// <summary>
/// Optional plugin that invokes package-supplied serialization logic (type + method from config).
/// When <see cref="SerializerTypeFullName"/> is empty, <see cref="Services.JsonMessageSerializer"/> is used.
/// </summary>
public sealed class SerializationPluginSettings
{
    /// <summary>Full CLR name of the class that performs serialization (from the loaded message package).</summary>
    public string? SerializerTypeFullName { get; init; }

    /// <summary>Name of the instance or static method to call.</summary>
    public string? SerializerMethodName { get; init; }

    /// <summary>When true, invokes <see cref="SerializerMethodName"/> as a static method on the type.</summary>
    public bool UseStaticMethod { get; init; }
}
