using Microsoft.Extensions.DependencyInjection;

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
            ServiceCollection.AddHttpClient<IPhotoService, PhotoService>();
        }
    }
}