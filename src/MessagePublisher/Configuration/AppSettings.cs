using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MessagePublisher.Configuration;

public static class AppSettings
{
    private static readonly Lazy<AppSettingsRoot> Loaded = new Lazy<AppSettingsRoot>(LoadFromFile);

    public static AppSettingsRoot Current => Loaded.Value;

    public static IReadOnlyDictionary<string, string> GetMsmqAddresses()
    {
        var source = Current.MSMQAddresses;
        if (source == null || source.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);
    }

    private static AppSettingsRoot LoadFromFile()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new AppSettingsRoot();

        var json = File.ReadAllText(path);
        var root = JsonConvert.DeserializeObject<AppSettingsRoot>(json);
        return root ?? new AppSettingsRoot();
    }
}
