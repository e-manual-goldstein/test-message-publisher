using System.Text.Json;
using System.Text.Json.Serialization;

namespace MessagePublisher.Services;

public sealed class JsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonMessageSerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public string Serialize(object instance, Type runtimeType) =>
        JsonSerializer.Serialize(runtimeType, instance, _options);
}
