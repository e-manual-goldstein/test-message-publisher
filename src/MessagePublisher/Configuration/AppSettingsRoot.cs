using System.Collections.Generic;

namespace MessagePublisher.Configuration;

/// <summary>
/// Shape of appsettings.json (no Microsoft.Extensions.Configuration on net452).
/// </summary>
public sealed class AppSettingsRoot
{
    public MessagePublisherSettings MessagePublisher { get; set; } = new();

    public Dictionary<string, string> MSMQAddresses { get; set; } = new();
}
