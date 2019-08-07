using Polly;
using System.Net.Http;

namespace ResilienceWithPolly.Console
{
    public interface IPollyHttpClientFactory
    {
        IAsyncPolicy<HttpResponseMessage> BuildPolicy(PollyPolicyType pollyPolicyType);
    }
}
