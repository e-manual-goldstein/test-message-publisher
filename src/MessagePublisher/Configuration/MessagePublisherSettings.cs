namespace MessagePublisher.Configuration;

public sealed class MessagePublisherSettings
{
    public SerializationPluginSettings SerializationPlugin { get; init; } = new();

    public DependencyInjectionSettings DependencyInjection { get; init; } = new();
}
