namespace MessagePublisher.Services;

/// <summary>
/// Picks runtime assemblies from an extracted nupkg layout (lib/TFM) with a sensible TFM fallback.
/// </summary>
public static class PackageAssemblyResolver
{
    private static readonly string[] PreferredTfms =
    [
        "net8.0",
        "net7.0",
        "net6.0",
        "netstandard2.1",
        "netstandard2.0",
        "net472",
        "net471",
        "net47",
        "net461",
        "net46",
        "net452",
        "net451",
        "net45"
    ];

    /// <summary>
    /// Returns the lib folder that best matches the host (net8) or falls back to older TFMs.
    /// </summary>
    public static string? ResolveLibDirectory(string extractedPackageRoot)
    {
        var libRoot = Path.Combine(extractedPackageRoot, "lib");
        if (!Directory.Exists(libRoot))
            return null;

        foreach (var tfm in PreferredTfms)
        {
            var candidate = Path.Combine(libRoot, tfm);
            if (Directory.Exists(candidate) && HasDlls(candidate))
                return candidate;
        }

        // Any TFM folder under lib/
        var any = Directory.GetDirectories(libRoot)
            .FirstOrDefault(d => HasDlls(d));
        return any;
    }

    private static bool HasDlls(string directory) =>
        Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly).Any();

    /// <summary>
    /// All managed assemblies in the chosen lib folder (top-level only).
    /// </summary>
    public static IReadOnlyList<string> GetAssemblyPaths(string? libDirectory)
    {
        if (libDirectory is null || !Directory.Exists(libDirectory))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(libDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
