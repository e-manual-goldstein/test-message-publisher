using System;

namespace Serialisation;

public interface ITextSerialiser
{
    string Serialise(object message, Type messageType);
}
