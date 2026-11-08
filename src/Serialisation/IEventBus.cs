namespace Serialisation;

public interface IEventBus
{
    void Publish(string eventName, object? payload = null);
}
