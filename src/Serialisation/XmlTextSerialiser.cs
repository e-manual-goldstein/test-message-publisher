using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Serialisation;

public sealed class XmlTextSerialiser : ITextSerialiser
{
    public string Serialise(object message, Type messageType)
    {
        ArgumentNullException.ThrowIfNull(message);

        var serializer = new XmlSerializer(messageType);
        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8
        };

        using var buffer = new StringWriter();
        using (var writer = XmlWriter.Create(buffer, settings))
        {
            serializer.Serialize(writer, message);
        }

        return buffer.ToString();
    }
}
