using Newtonsoft.Json;
using ResilienceWithPolly.Console.Model;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResilienceWithPolly.Console
{
public class PhotoService : IPhotoService
{
    private readonly HttpClient _httpClient;
    private readonly IPollyHttpClientFactory _pollyHttpClientFactory;

    public PhotoService(HttpClient httpClient, IPollyHttpClientFactory pollyHttpClientFactory)
    {
        _httpClient = httpClient;
        _pollyHttpClientFactory = pollyHttpClientFactory;
    }

    public async Task<IReadOnlyList<Album>> GetAllAlbumsAsync()
    {
        var policy = _pollyHttpClientFactory.BuildPolicy(PollyPolicyType.AlbumServicePolicy);

        var uri = "https://jsonplaceholder.typicode.com/albums";
        var response = await policy.ExecuteAsync(() => _httpClient.GetAsync(uri));
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<Album>>(responseString);
            return result;
        }

        return new List<Album>();
    }
}
}