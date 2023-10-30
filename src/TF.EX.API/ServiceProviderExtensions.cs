using Microsoft.Extensions.DependencyInjection;
using TF.EX.Domain.Ports;

namespace TF.EX.API
{
    public static class ServiceProviderExtensions
    {
        public static void AddAPIManager(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IAPIManager, APIManager>();
        }
    }
}
