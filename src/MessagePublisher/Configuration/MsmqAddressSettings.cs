using System.Collections.Generic;

namespace MessagePublisher.Configuration;

/// <summary>
/// Routing key → MSMQ queue path mappings from appsettings.json.
/// </summary>
public sealed class MsmqAddressSettings
{
    public const string SectionName = "MSMQAddresses";

    public Dictionary<string, string> Addresses { get; set; } = new();
}
