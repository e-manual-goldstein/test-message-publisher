using System.Text.Json;
using System.Text.Json.Serialization;

namespace MessagePublisher.Configuration;

public static class SettingsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public const string DefaultFileName = "messagepublisher.settings.json";

    public static MessagePublisherSettings Load(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, DefaultFileName);
        if (!File.Exists(path))
            return new MessagePublisherSettings();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MessagePublisherSettings>(json, JsonOptions)
               ?? new MessagePublisherSettings();
    }
}
