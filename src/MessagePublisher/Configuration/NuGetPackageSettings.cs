namespace MessagePublisher.Configuration;

/// <summary>
/// Package ids and base type defaults (from appsettings.json).
/// </summary>
public sealed class NuGetPackageSettings
{
    public const string SectionName = "NuGet";

    /// <summary>Package "A" — concrete message types.</summary>
    public string MessageTypesPackageId { get; set; } = "";

    /// <summary>Package "B" — assembly that defines the message base type.</summary>
    public string BaseTypePackageId { get; set; } = "";

    public string BaseTypeFullName { get; set; } = "";

    /// <summary>Optional: match a feed <see cref="NuGet.Configuration.PackageSource.Name"/> from NuGet.config.</summary>
    public string PreferredFeedName { get; set; } = "";
}
