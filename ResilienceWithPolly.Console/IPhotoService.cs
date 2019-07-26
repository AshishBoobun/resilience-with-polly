using System.Collections.Generic;
using System.Threading.Tasks;
using ResilienceWithPolly.Console.Model;

namespace ResilienceWithPolly.Console
{
    public interface IPhotoService
    {
       Task<IReadOnlyList<Album>> GetAllAlbumsAsync();
    }
}