using System;

namespace MessagePublisher.Composition;

internal sealed class ServiceDescriptor
{
    public Type ServiceType { get; }
    public Type ImplementationType { get; }
    public object? Instance { get; }

    private ServiceDescriptor(Type serviceType, Type implementationType, object? instance)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Instance = instance;
    }

    public static ServiceDescriptor ForInstance(Type serviceType, object instance)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        return new ServiceDescriptor(serviceType, instance.GetType(), instance);
    }

    public static ServiceDescriptor ForImplementation(Type serviceType, Type implementationType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationType == null)
            throw new ArgumentNullException(nameof(implementationType));

        return new ServiceDescriptor(serviceType, implementationType, null);
    }
}
