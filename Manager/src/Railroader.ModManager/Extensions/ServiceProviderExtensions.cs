using System;

namespace Railroader.ModManager.Extensions;

public static class ServiceProviderExtensions
{
    public static TInterface GetService<TInterface>(this IServiceProvider serviceProvider) => (TInterface)serviceProvider.GetService(typeof(TInterface))!;
}
