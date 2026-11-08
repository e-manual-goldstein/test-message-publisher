using System;

namespace MessagePublisher.Services;

/// <summary>
/// Serialises via the in-repo <see cref="Serialisation"/> module (stand-in for package C).
/// </summary>
public sealed class ModuleMessageSerializer : IMessageSerializer
{
    private readonly IMessageBuilder _messageBuilder;

    public ModuleMessageSerializer(IMessageBuilder messageBuilder)
    {
        _messageBuilder = messageBuilder ?? throw new ArgumentNullException(nameof(messageBuilder));
    }

    public string Serialize(object instance, Type runtimeType)
    {
        if (instance is not BaseMessage baseMessage)
        {
            throw new InvalidOperationException(
                $"Expected {typeof(BaseMessage).FullName}; got {runtimeType.FullName}.");
        }

        return _messageBuilder.BuildPayload(baseMessage);
    }
}
