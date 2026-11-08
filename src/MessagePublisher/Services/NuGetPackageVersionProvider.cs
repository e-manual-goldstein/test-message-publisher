using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MessagePublisher.Services;

public static class NuGetPackageVersionProvider
{
    public static async Task<IReadOnlyList<string>> GetVersionsAsync(
        NuGetFeedOption feed,
        string packageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return Array.Empty<string>();

        var repository = feed.CreateRepository();
        var finder = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        if (finder is null)
            return Array.Empty<string>();

        using var cache = new SourceCacheContext();
        var versions = await finder.GetAllVersionsAsync(packageId.Trim(), cache, NullLogger.Instance, cancellationToken);

        return versions
            .OrderByDescending(v => v, VersionComparer.Default)
            .Select(v => v.ToString())
            .Distinct()
            .ToList();
    }
}
