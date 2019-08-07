using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResilienceWithPolly.Console
{
    public class Bootstrapper
    {
        public ServiceCollection ServiceCollection { get; }

        public ServiceProvider ServiceProvider { get; }

        public Bootstrapper()
        {
            ServiceCollection = new ServiceCollection();
            Initialise();
            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        private void Initialise()
        {
            ServiceCollection.AddSingleton<IPollyHttpClientFactory, PollyHttpClientFactory>();
            ServiceCollection.AddHttpClient<IPhotoService, PhotoService>();
        }
    }
}