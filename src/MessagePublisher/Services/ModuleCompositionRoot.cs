#if !IMPORT_PRIVATE_NAMESPACES
namespace MessagePublisher.Services;

/// <summary>
/// Simple composition root for the Serialisation module (swap types via GlobalUsings when referencing real package C).
/// </summary>
public static class ModuleCompositionRoot
{
    public static IMessageSerializer CreateSerializer()
    {
        throw new NotImplementedException();
    }
}
#endif