using System;

namespace Railroader.ModManager.Extensions;

internal static class ServiceProviderExtensions
{
    public static TInterface GetService<TInterface>(this IServiceProvider serviceProvider) => (TInterface)serviceProvider.GetService(typeof(TInterface))!;
}
