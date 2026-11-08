namespace MessagePublisher.Configuration;

public enum ServiceLifetime
{
    Singleton,
    Transient
}

/// <summary>
/// Spring-style bean registration: maps an abstraction to an implementation loaded from the package.
/// </summary>
public sealed class ServiceRegistrationSettings
{
    /// <summary>Optional service key (e.g. contract). When set, the DI container resolves this type to the implementation.</summary>
    public string? ServiceTypeFullName { get; init; }

    /// <summary>Concrete implementation type full name.</summary>
    public required string ImplementationTypeFullName { get; init; }

    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Singleton;
}
