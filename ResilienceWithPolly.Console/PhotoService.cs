using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ResilienceWithPolly.Console.Model;

namespace ResilienceWithPolly.Console
{
    public class PhotoService : IPhotoService
    {
        private readonly HttpClient _httpClient;

        public PhotoService(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public async Task<IReadOnlyList<Album>> GetAllAlbumsAsync()
        {
            var uri = "https://jsonplaceholder.typicode.com/albums";
            var responseString = await _httpClient.GetStringAsync(uri);
            var result = JsonConvert.DeserializeObject<List<Album>>(responseString);

            return result;
        }
    }
}