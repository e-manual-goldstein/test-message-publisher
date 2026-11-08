namespace MessagePublisher.Configuration;

public sealed class DependencyInjectionSettings
{
    public List<ServiceRegistrationSettings> Services { get; init; } = [];
}
