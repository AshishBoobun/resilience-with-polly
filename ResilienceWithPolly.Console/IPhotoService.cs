using System.Collections.Generic;
using System.Threading.Tasks;
using ResilienceWithPolly.Console.Model;

namespace ResilienceWithPolly.Console
{
    public interface IPhotoService
    {
        Task<IReadOnlyList<Photo>> GetAllPhotosAsync();

        Task<IReadOnlyList<Album>> GetAllAlbumsAsync();
    }
}