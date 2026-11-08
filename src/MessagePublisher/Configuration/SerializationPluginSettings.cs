namespace MessagePublisher.Configuration;

/// <summary>
/// Optional plugin that invokes package-supplied serialization logic (type + method from config).
/// When <see cref="SerializerTypeFullName"/> is empty, <see cref="Services.JsonMessageSerializer"/> is used.
/// </summary>
public sealed class SerializationPluginSettings
{
    public const string SectionName = "SerializationPlugin";

    /// <summary>Full CLR name of the class that performs serialization (usually in package B).</summary>
    public string SerializerTypeFullName { get; set; } = "";

    /// <summary>Name of the instance or static method to call.</summary>
    public string SerializerMethodName { get; set; } = "";

    /// <summary>When true, invokes <see cref="SerializerMethodName"/> as a static method on the type.</summary>
    public bool UseStaticMethod { get; set; }
}
