using Microsoft.Extensions.DependencyInjection;

namespace MessagePublisher.Composition;

/// <summary>
/// Register application services for the WPF host (called from <see cref="MainViewModel"/>).
/// </summary>
internal static class ViewModelServiceConfigurator
{
    internal static void Configure(IServiceCollection services)
    {
        // Registrations go here.
    }
}
