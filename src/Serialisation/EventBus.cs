namespace Serialisation;

/// <summary>
/// Minimal in-process event bus used while building wire payloads (stand-in for production infrastructure).
/// </summary>
public sealed class EventBus : IEventBus
{
    public void Publish(string eventName, object? payload = null)
    {
        // Deliberately lightweight: hook point for real auditing/telemetry later.
        _ = (eventName, payload);
    }
}
