using System.Reflection;
using NuGet.Configuration;
using NuGet.Protocol;

namespace MessagePublisher.Services;

/// <summary>
/// Lists enabled package sources the same way Visual Studio / dotnet nuget do (NuGet.config hierarchy).
/// </summary>
public static class NuGetConfigFeedProvider
{
    public static IReadOnlyList<NuGetFeedOption> GetEnabledFeeds()
    {
        var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var settings = Settings.LoadDefaultSettings(root);
        var provider = new PackageSourceProvider(settings);
        var sources = provider.LoadPackageSources();

        return sources
            .Where(s => s.IsEnabled && !string.IsNullOrWhiteSpace(s.Source))
            .Select(s => new NuGetFeedOption(s))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

/// <summary>
/// One entry from NuGet.config packageSources (may include stored credentials).
/// </summary>
public sealed class NuGetFeedOption
{
    public NuGetFeedOption(PackageSource source)
    {
        Source = source;
        Name = source.Name;
        SourceUrl = source.Source;
    }

    public PackageSource Source { get; }

    public string Name { get; }

    public string SourceUrl { get; }

    public SourceRepository CreateRepository() =>
        new(Source, Repository.Provider.GetCoreV3());

    public override string ToString() => Name;
}
