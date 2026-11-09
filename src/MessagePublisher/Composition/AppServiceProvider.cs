using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MessagePublisher.Composition;

/// <summary>
/// Minimal net452-friendly service provider with singleton constructor injection (no Microsoft.Extensions.DependencyInjection).
/// </summary>
public sealed class AppServiceProvider
{
    private readonly Dictionary<Type, ServiceDescriptor> _descriptors;
    private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
    private readonly List<Type> _resolutionChain = new List<Type>();

    internal AppServiceProvider(Dictionary<Type, ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
    }

    public object GetRequiredService(Type serviceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));

        if (_singletons.TryGetValue(serviceType, out var cached))
            return cached;

        if (!_descriptors.TryGetValue(serviceType, out var descriptor))
        {
            throw new InvalidOperationException(
                $"Service of type '{serviceType.FullName}' is not registered.");
        }

        if (descriptor.Instance != null)
        {
            _singletons[serviceType] = descriptor.Instance;
            return descriptor.Instance;
        }

        if (_resolutionChain.Contains(serviceType))
        {
            var chain = new StringBuilder();
            foreach (var type in _resolutionChain)
            {
                if (chain.Length > 0)
                    chain.Append(" -> ");
                chain.Append(type.Name);
            }

            chain.Append(" -> ").Append(serviceType.Name);
            throw new InvalidOperationException($"Circular dependency detected: {chain}.");
        }

        _resolutionChain.Add(serviceType);
        try
        {
            var instance = CreateInstance(descriptor.ImplementationType);
            _singletons[serviceType] = instance;
            return instance;
        }
        finally
        {
            _resolutionChain.RemoveAt(_resolutionChain.Count - 1);
        }
    }

    public T GetRequiredService<T>() where T : class => (T)GetRequiredService(typeof(T));

    private object CreateInstance(Type implementationType)
    {
        var constructors = implementationType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        if (constructors.Length == 0)
            throw new InvalidOperationException(
                $"Type '{implementationType.FullName}' has no public instance constructors.");

        var constructor = constructors
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            args[i] = GetRequiredService(parameters[i].ParameterType);

        try
        {
            return constructor.Invoke(args);
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException(
                $"Failed to create an instance of '{implementationType.FullName}'.",
                ex.InnerException ?? ex);
        }
    }
}
