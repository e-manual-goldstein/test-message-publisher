#if !IMPORT_PRIVATE_NAMESPACES
using MessagePublisher.Services;

namespace MessagePublisher.Composition;

/// <summary>
/// Register application services for the WPF host (called from <see cref="MainViewModel.MainViewModel"/>).
/// </summary>
internal static class ServiceConfigurator
{
    internal static void Configure(AppServiceCollection services)
    {
        services.AddSingleton<IMSMQSenderService, MSMQSendPort>();
        services.AddSingleton<IMsgRouteEngine, MsgRouteEngine>();
    }
}
#endif