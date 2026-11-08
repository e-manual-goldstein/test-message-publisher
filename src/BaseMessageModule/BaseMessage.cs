namespace BaseMessageModule;

/// <summary>
/// Stand-in for the proprietary base message type (swap namespace via GlobalUsings when wiring real packages).
/// </summary>
public abstract class BaseMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();

    public string? CorrelationId { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public string Source { get; set; } = "";

    public int SchemaVersion { get; set; } = 1;

    public string? TenantId { get; set; }
}
