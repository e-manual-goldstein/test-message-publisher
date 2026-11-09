using System;
using System.Collections.Generic;

namespace MessagePublisher.Composition;

/// <summary>
/// Minimal net452-friendly substitute for resolving services (no Microsoft.Extensions.DependencyInjection).
/// </summary>
public sealed class AppServiceProvider
{
    private readonly Dictionary<Type, Func<object>> _registrations;

    internal AppServiceProvider(Dictionary<Type, Func<object>> registrations)
    {
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
    }

    public object GetRequiredService(Type serviceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));

        if (!_registrations.TryGetValue(serviceType, out var factory))
        {
            throw new InvalidOperationException(
                $"Service of type '{serviceType.FullName}' is not registered.");
        }

        return factory();
    }

    public T GetRequiredService<T>() where T : class => (T)GetRequiredService(typeof(T));
}
