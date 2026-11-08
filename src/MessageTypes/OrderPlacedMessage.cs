using BaseMessageModule;

namespace MessageTypes;

public sealed class OrderPlacedMessage : Event
{
    public string OrderId { get; set; } = "";

    public string CustomerId { get; set; } = "";

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "GBP";
}
