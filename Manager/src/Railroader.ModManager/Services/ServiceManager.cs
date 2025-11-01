using System;
using System.Collections.Generic;

namespace Railroader.ModManager.Services;

public sealed class ServiceManager : IServiceProvider
{
    public sealed record ServiceContext(Func<IServiceProvider, object?> Factory, bool IsSingleton, object? Instance);

    public Dictionary<Type, ServiceContext> Services { get; } = new();

    public void AddSingleton<TInterface, TService>() where TService : class, TInterface, new() => Add<TInterface>(_ => new TService(), true);
    public void AddTransient<TInterface, TService>() where TService : class, TInterface, new() => Add<TInterface>(_ => new TService(), false);

    public void AddSingleton<TInterface, TService>(Func<IServiceProvider, TService> factory) where TService : class, TInterface => Add<TInterface>(factory, true);
    public void AddTransient<TInterface, TService>(Func<IServiceProvider, TService> factory) where TService : class, TInterface => Add<TInterface>(factory, false);

    private void Add<TInterface>(Func<IServiceProvider, object?> factory, bool isSingleton) {
        lock (Services) {
            if (Services.ContainsKey(typeof(TInterface))) {
                throw new InvalidOperationException($"Service {typeof(TInterface)} is already registered");
            }

            Services.Add(typeof(TInterface), new ServiceContext(factory, isSingleton, null));
        }
    }

    public object GetService(Type serviceType) {
        lock (Services) {
            if (!Services.TryGetValue(serviceType, out var serviceContext)) {
                throw new InvalidOperationException($"Cannot find service {serviceType}");
            }

            if (serviceContext.Instance != null) {
                return serviceContext.Instance;
            }

            var instance = serviceContext.Factory(this) ?? throw new InvalidOperationException($"Failed to create instance of {serviceType}");
            if (serviceContext.IsSingleton) {
                Services[serviceType] = serviceContext with { Instance = instance };
            }

            return instance;
        }
    }
}
