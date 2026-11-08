using BaseMessageModule;

namespace MessageTypes;

public sealed class CustomerUpdatedMessage : Event
{
    public string CustomerId { get; set; } = "";

    public string Email { get; set; } = "";

    public bool MarketingOptIn { get; set; }
}
