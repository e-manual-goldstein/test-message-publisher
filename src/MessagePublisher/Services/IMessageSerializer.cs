namespace MessagePublisher.Services;

/// <summary>
/// Serializes message instances to strings for transport.
/// </summary>
public interface IMessageSerializer
{
    string Serialize(object instance, Type runtimeType);
}
