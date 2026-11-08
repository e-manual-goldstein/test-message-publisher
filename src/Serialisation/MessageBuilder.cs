using System;

namespace Serialisation;

public sealed class MessageBuilder : IMessageBuilder
{
    private readonly ITextSerialiser _textSerialiser;
    private readonly IEventBus _eventBus;

    public MessageBuilder(ITextSerialiser textSerialiser, IEventBus eventBus)
    {
        _textSerialiser = textSerialiser ?? throw new ArgumentNullException(nameof(textSerialiser));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public string BuildPayload(BaseMessageModule.Event message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var runtimeType = message.GetType();
        _eventBus.Publish("Message.PayloadBuilding", new { runtimeType.Name, message.MessageId });
        return _textSerialiser.Serialise(message, runtimeType);
    }
}
