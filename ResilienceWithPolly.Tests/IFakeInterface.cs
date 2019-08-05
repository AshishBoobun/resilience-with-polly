using System.Net.Http;
using System.Threading.Tasks;

namespace ResilienceWithPolly.Tests
{
    public interface IFakeInterface
    {
        Task<HttpResponseMessage> DoSomethingAsync(int count);
    }
}