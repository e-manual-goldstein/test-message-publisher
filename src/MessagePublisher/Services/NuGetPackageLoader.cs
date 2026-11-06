using System.IO.Compression;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MessagePublisher.Services;

/// <summary>
/// Downloads a .nupkg from a NuGet v3 feed (e.g. Nexus) and extracts it to a cache directory.
/// </summary>
public static class NuGetPackageLoader
{
    /// <param name="feedUrl">V3 service index URL, e.g. https://your-nexus/repository/nuget-org/v3/index.json</param>
    /// <param name="packageId">Package id.</param>
    /// <param name="version">SemVer version string.</param>
    /// <param name="outputDirectory">Folder to extract into (created if missing).</param>
    /// <param name="username">Optional feed username.</param>
    /// <param name="password">Optional feed password or API token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<string> DownloadAndExtractAsync(
        string feedUrl,
        string packageId,
        string version,
        string outputDirectory,
        string? username,
        string? password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
            throw new ArgumentException("Feed URL is required.", nameof(feedUrl));
        if (string.IsNullOrWhiteSpace(packageId))
            throw new ArgumentException("Package id is required.", nameof(packageId));
        if (!NuGetVersion.TryParse(version, out var nuGetVersion))
            throw new ArgumentException($"Invalid NuGet version: {version}", nameof(version));

        Directory.CreateDirectory(outputDirectory);

        var source = new PackageSource(feedUrl);
        if (!string.IsNullOrEmpty(username))
        {
            source.Credentials = new PackageSourceCredential(
                feedUrl,
                username,
                password ?? string.Empty,
                isPasswordClearText: true);
        }

        var providers = Repository.Provider.GetCoreV3();
        var repository = new SourceRepository(source, providers);
        var finder = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken)
            ?? throw new InvalidOperationException("This feed does not support FindPackageById (v3 resource missing).");

        using var cache = new SourceCacheContext();
        var logger = NullLogger.Instance;

        var exists = await finder.DoesPackageExistAsync(packageId, nuGetVersion, cache, logger, cancellationToken);
        if (!exists)
            throw new InvalidOperationException($"Package {packageId} {nuGetVersion} was not found on the feed.");

        var nupkgPath = Path.Combine(outputDirectory, $"{packageId}.{nuGetVersion}.nupkg");
        await using (var fileStream = File.Create(nupkgPath))
        {
            var copied = await finder.CopyNupkgToStreamAsync(
                packageId,
                nuGetVersion,
                fileStream,
                cache,
                logger,
                cancellationToken);
            if (!copied)
                throw new InvalidOperationException($"Failed to download {packageId} {nuGetVersion}.");
        }

        var extractRoot = Path.Combine(outputDirectory, "extracted");
        if (Directory.Exists(extractRoot))
            Directory.Delete(extractRoot, recursive: true);
        Directory.CreateDirectory(extractRoot);

        ZipFile.ExtractToDirectory(nupkgPath, extractRoot);

        return extractRoot;
    }
}
