using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResilienceWithPolly.Console
{
    public class PollyHttpClientFactory : IPollyHttpClientFactory
    {
        private static readonly Lazy<IAsyncPolicy<HttpResponseMessage>> AlbumServicePolicyLazy = new Lazy<IAsyncPolicy<HttpResponseMessage>>(() => HttpClientPollyPolicy.Initialise()
                .AddWaitRetryPolicy(2)
                .AddFallbackPolicy(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
                .AddCircuitBreakerPolicy(5, TimeSpan.FromSeconds(40))
                .AddTimeoutPolicy(60)
                .Build());

        public IAsyncPolicy<HttpResponseMessage> BuildPolicy(PollyPolicyType pollyPolicyType)
        {
            switch (pollyPolicyType)
            {
                case PollyPolicyType.AlbumServicePolicy:
                    return AlbumServicePolicyLazy.Value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pollyPolicyType), "Invalid PollyPolicyType value");
            }
        }
    }
}
