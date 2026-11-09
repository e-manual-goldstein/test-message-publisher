#if !IMPORT_PRIVATE_NAMESPACES
namespace MessagePublisher.Composition;

/// <summary>
/// Register application services for the WPF host (called from <see cref="MainViewModel.MainViewModel"/>).
/// </summary>
internal static class ServiceConfigurator
{
    internal static void Configure(AppServiceCollection services)
    {
        // Registrations go here.
    }
}
#endif