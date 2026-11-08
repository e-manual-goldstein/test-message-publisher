using BaseMessageModule;

namespace MessageTypes;

public sealed class CustomerUpdatedMessage : BaseMessage
{
    public string CustomerId { get; set; } = "";

    public string Email { get; set; } = "";

    public bool MarketingOptIn { get; set; }
}
