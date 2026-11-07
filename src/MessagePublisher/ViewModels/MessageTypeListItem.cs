using MessagePublisher.Services;

namespace MessagePublisher.ViewModels;

/// <summary>
/// One row in the message type list (short name for display).
/// </summary>
public sealed class MessageTypeListItem
{
    public MessageTypeListItem(DiscoveredMessageType discovered)
    {
        Discovered = discovered;
        ShortName = discovered.ShortName;
    }

    public DiscoveredMessageType Discovered { get; }

    public string ShortName { get; }
}
