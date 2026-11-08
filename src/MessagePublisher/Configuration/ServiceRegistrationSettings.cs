namespace MessagePublisher.Configuration;

public enum ServiceLifetime
{
    Singleton,
    Transient
}

/// <summary>
/// Spring-style bean registration: maps an abstraction to an implementation loaded from packages A/B.
/// </summary>
public sealed class ServiceRegistrationSettings
{
    /// <summary>Optional service key (e.g. contract). When set, the DI container resolves this type to the implementation.</summary>
    public string ServiceTypeFullName { get; set; } = "";

    /// <summary>Concrete implementation type full name.</summary>
    public string ImplementationTypeFullName { get; set; } = "";

    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
}
