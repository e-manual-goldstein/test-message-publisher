using System.Reflection;
using System.Runtime.Loader;

namespace MessagePublisher.Services;

/// <summary>
/// Collectible load context for message assemblies and their dependencies shipped beside them.
/// </summary>
public sealed class MessagePackageLoadContext : AssemblyLoadContext
{
    private readonly string[] _probeDirectories;

    public MessagePackageLoadContext(string libDirectory)
        : this([libDirectory])
    {
    }

    public MessagePackageLoadContext(IEnumerable<string> libDirectories) : base(isCollectible: true)
    {
        var probe = new List<string>();
        foreach (var libDirectory in libDirectories)
        {
            if (string.IsNullOrEmpty(libDirectory))
                continue;

            probe.Add(libDirectory);
            var extractedRoot = Directory.GetParent(libDirectory)?.FullName;
            if (!string.IsNullOrEmpty(extractedRoot))
                probe.Add(extractedRoot);
        }

        if (probe.Count == 0)
            throw new ArgumentException("At least one lib directory is required.", nameof(libDirectories));

        _probeDirectories = probe.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (string.IsNullOrEmpty(name))
            return null;

        foreach (var dir in _probeDirectories)
        {
            var path = Path.Combine(dir, name + ".dll");
            if (File.Exists(path))
                return LoadFromAssemblyPath(path);
        }

        return null;
    }
}
