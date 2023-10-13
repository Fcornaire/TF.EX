using Microsoft.Extensions.DependencyInjection;
using System;

namespace TF.EX.API
{
    public static class ServiceProviderExtensions
    {
        public static void AddAPIManager(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IAPIManager, APIManager>();
        }

        public static IAPIManager ResolveAPIManager(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IAPIManager>();
        }
    }
}
