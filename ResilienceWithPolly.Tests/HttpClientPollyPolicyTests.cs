using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using ResilienceWithPolly.Console;
using Xunit;

namespace ResilienceWithPolly.Tests
{
    public class HttpClientPollyPolicyTests
    {
        [Fact]
        public async Task AddWaitRetryPolicy_CallSucceedInitially_DoesNotRetry()
        {
            var policy = HttpClientPollyPolicy.Initialise()
                .AddWaitRetryPolicy(2)
                .Build();
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            var mockInstance = mockInterface.Object;

            var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        }

        [Fact]
        public async Task AddWaitRetryPolicy_ThrowNullException_DoesNotRetry()
        {
            var retryCount = 2;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddWaitRetryPolicy(retryCount)
                .Build();
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Throws(new NullReferenceException());
            var mockInstance = mockInterface.Object;

            Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            await Assert.ThrowsAsync<NullReferenceException>(action);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Redirect)]
        [InlineData(HttpStatusCode.SwitchingProtocols)]
        public async Task AddWaitRetryPolicy_ReturnHttpNonTransientError_DoesNotRetry(HttpStatusCode transientStatusCode)
        {
            var retryCount = 2;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddWaitRetryPolicy(retryCount)
                .Build();
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(new HttpResponseMessage(transientStatusCode)));
            var mockInstance = mockInterface.Object;

            var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(transientStatusCode, result.StatusCode);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        }

        [Theory]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.RequestTimeout)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task AddWaitRetryPolicy_ReturnHttpTransientError_RetryUptoCount(HttpStatusCode transientStatusCode)
        {
            var retryCount = 2;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddWaitRetryPolicy(retryCount)
                .Build();
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(new HttpResponseMessage(transientStatusCode)));
            var mockInstance = mockInterface.Object;

            var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(transientStatusCode, result.StatusCode);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(retryCount + 1));
        }

        [Fact]
        public async Task AddWaitRetryPolicy_ThrowHttpRequestException_RetryUptoCount()
        {
            var retryCount = 2;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddWaitRetryPolicy(retryCount)
                .Build();
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Throws(new HttpRequestException());
            var mockInstance = mockInterface.Object;

            Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            await Assert.ThrowsAsync<HttpRequestException>(action);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(retryCount + 1));
        }
    }
}