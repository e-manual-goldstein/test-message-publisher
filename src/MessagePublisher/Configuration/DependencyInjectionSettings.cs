namespace MessagePublisher.Configuration;

public sealed class DependencyInjectionSettings
{
    public const string SectionName = "DependencyInjection";

    public List<ServiceRegistrationSettings> Services { get; set; } = [];
}
