using System;
using System.Collections.Generic;

namespace MessagePublisher.Composition;

/// <summary>
/// Minimal net452-friendly substitute for <c>IServiceCollection</c> (no Microsoft.Extensions.DependencyInjection).
/// </summary>
public sealed class AppServiceCollection
{
    private readonly Dictionary<Type, ServiceDescriptor> _descriptors = new Dictionary<Type, ServiceDescriptor>();

    public void AddSingleton<TService>(TService instance) where TService : class
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        _descriptors[typeof(TService)] = ServiceDescriptor.ForInstance(typeof(TService), instance);
    }

    public void AddSingleton<TService>() where TService : class
    {
        AddSingleton<TService, TService>();
    }

    public void AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _descriptors[typeof(TService)] =
            ServiceDescriptor.ForImplementation(typeof(TService), typeof(TImplementation));
    }

    internal IReadOnlyDictionary<Type, ServiceDescriptor> Descriptors => _descriptors;

    public AppServiceProvider BuildServiceProvider() => new AppServiceProvider(_descriptors);
}
