namespace MessagePublisher.Services;

/// <summary>
/// Simple composition root for the Serialisation module (swap types via GlobalUsings when referencing real package C).
/// </summary>
public static class ModuleCompositionRoot
{
    public static IMessageSerializer CreateSerializer()
    {
        IEventBus eventBus = new EventBus();
        ITextSerialiser textSerialiser = new XmlTextSerialiser();
        IMessageBuilder messageBuilder = new MessageBuilder(textSerialiser, eventBus);
        return new ModuleMessageSerializer(messageBuilder);
    }
}
