using System;
using System.Collections.Generic;

namespace MessagePublisher.Composition;

/// <summary>
/// Minimal net452-friendly substitute for <c>IServiceCollection</c> (no Microsoft.Extensions.DependencyInjection).
/// </summary>
public sealed class AppServiceCollection
{
    private readonly Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();

    public void AddSingleton<TService>(TService instance) where TService : class
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        _registrations[typeof(TService)] = () => instance;
    }

    public void AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService, new()
    {
        object? instance = null;
        _registrations[typeof(TService)] = () =>
        {
            if (instance == null)
                instance = new TImplementation();
            return instance;
        };
    }

    public AppServiceProvider BuildServiceProvider() => new AppServiceProvider(_registrations);
}
