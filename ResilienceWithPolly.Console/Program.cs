using System.Threading.Tasks;

namespace ResilienceWithPolly.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = new Bootstrapper().ServiceProvider;
            var photoService = (IPhotoService)serviceProvider.GetService(typeof(IPhotoService));

            var albums = await photoService.GetAllAlbumsAsync();
            foreach (var album in albums)
            {
                System.Console.WriteLine($"Id: {album.Id}, Title: {album.Title}");
            }
        }
    }
}
