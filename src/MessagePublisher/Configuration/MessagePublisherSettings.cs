namespace MessagePublisher.Configuration;

/// <summary>
/// Host defaults from appsettings.json.
/// </summary>
public sealed class MessagePublisherSettings
{
    public const string SectionName = "MessagePublisher";

    public string BaseTypeFullName { get; set; } = "BaseMessageModule.BaseMessage";
}
