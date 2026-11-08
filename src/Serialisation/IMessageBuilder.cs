using BaseMessageModule;

namespace Serialisation;

public interface IMessageBuilder
{
    string BuildPayload(BaseMessage message);
}
