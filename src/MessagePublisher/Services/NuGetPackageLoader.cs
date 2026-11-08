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
    public static Task<string> DownloadAndExtractAsync(
        NuGetFeedOption feed,
        string packageId,
        string version,
        string outputDirectory,
        string? credentialOverrideUser,
        string? credentialOverridePassword,
        CancellationToken cancellationToken = default)
    {
        var source = feed.Source;
        if (!string.IsNullOrWhiteSpace(credentialOverrideUser))
        {
            source = new PackageSource(source.Source, source.Name, isEnabled: true, isOfficial: false)
            {
                Credentials = new PackageSourceCredential(
                    source.Source,
                    credentialOverrideUser,
                    credentialOverridePassword ?? string.Empty,
                    isPasswordClearText: true,
                    validAuthenticationTypesText: null)
            };
        }

        return DownloadAndExtractAsync(source, packageId, version, outputDirectory, cancellationToken);
    }

    public static async Task<string> DownloadAndExtractAsync(
        PackageSource source,
        string packageId,
        string version,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source.Source))
            throw new ArgumentException("Package source URL is required.", nameof(source));
        if (string.IsNullOrWhiteSpace(packageId))
            throw new ArgumentException("Package id is required.", nameof(packageId));
        if (!NuGetVersion.TryParse(version, out var nuGetVersion))
            throw new ArgumentException($"Invalid NuGet version: {version}", nameof(version));

        Directory.CreateDirectory(outputDirectory);

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
