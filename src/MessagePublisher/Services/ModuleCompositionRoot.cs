#if !IMPORT_PRIVATE_NAMESPACE
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