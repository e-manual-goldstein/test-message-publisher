using System.Reflection;
using System.Runtime.Loader;

namespace MessagePublisher.Services;

/// <summary>
/// Collectible load context for message assemblies and their dependencies shipped beside them.
/// </summary>
public sealed class MessagePackageLoadContext : AssemblyLoadContext
{
    private readonly string[] _probeDirectories;

    public MessagePackageLoadContext(string libDirectory) : base(isCollectible: true)
    {
        if (string.IsNullOrEmpty(libDirectory))
            throw new ArgumentException("Lib directory is required.", nameof(libDirectory));

        // Probe lib folder and package root (for runtimes/ etc.) — keep simple: lib + parent extracted root.
        var extractedRoot = Directory.GetParent(libDirectory)?.FullName ?? libDirectory;
        _probeDirectories = [libDirectory, extractedRoot];
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

        // Framework / shared runtime assemblies (e.g. System.*) resolve from the default context.
        return null;
    }
}
