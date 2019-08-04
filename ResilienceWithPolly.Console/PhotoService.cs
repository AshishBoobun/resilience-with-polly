using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using Polly.Timeout;
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

        private static IAsyncPolicy<HttpResponseMessage> GetHttpClientWaitRetryPolicy(int retryCount)
        {
            var policy = Policy.Handle<HttpRequestException>()
            .OrResult(TransientHttpStatusCodePredicate)
            .WaitAndRetryAsync(retryCount, ExponentialBackoffTimespan)

            return policy;
        }

        private static IAsyncPolicy GetHttpClientTimeoutPolicy(int timeoutInSeconds)
        {
            var policy = Policy.TimeoutAsync(timeoutInSeconds, TimeoutStrategy.Pessimistic)

            return policy;
        }



        private static readonly Func<HttpResponseMessage, bool> TransientHttpStatusCodePredicate = (response) =>
        {
            return (int)response.StatusCode >= 500
                || response.StatusCode == HttpStatusCode.RequestTimeout
                || response.StatusCode == HttpStatusCode.TooManyRequests;
        };

        private static Func<int, TimeSpan> ExponentialBackoffTimespan = retryNumber =>
        {
            return TimeSpan.FromSeconds(Math.Pow(2, retryNumber));
        };

    }
}