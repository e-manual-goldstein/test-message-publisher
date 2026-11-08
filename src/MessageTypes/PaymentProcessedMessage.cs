using BaseMessageModule;

namespace MessageTypes;

public sealed class PaymentProcessedMessage : BaseMessage
{
    public string PaymentId { get; set; } = "";

    public string OrderId { get; set; } = "";

    public decimal Amount { get; set; }

    public bool Succeeded { get; set; }
}
