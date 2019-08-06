using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly.CircuitBreaker;
using Polly.Timeout;
using ResilienceWithPolly.Console;
using Xunit;

namespace ResilienceWithPolly.Tests
{
    public class HttpClientPollyPolicyTests
    {
        // [Fact]
        // public async Task AddWaitRetryPolicy_CallSucceedInitially_DoesNotRetry()
        // {
        //     var policy = HttpClientPollyPolicy.Initialise()
        //         .AddWaitRetryPolicy(2)
        //         .Build();
        //     var mockInterface = new Mock<IFakeInterface>();
        //     mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
        //         .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        //     var mockInstance = mockInterface.Object;

        //     var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

        //     Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        //     mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        // }

        // [Fact]
        // public async Task AddWaitRetryPolicy_ThrowNullException_DoesNotRetry()
        // {
        //     var retryCount = 2;
        //     var policy = HttpClientPollyPolicy.Initialise()
        //         .AddWaitRetryPolicy(retryCount)
        //         .Build();
        //     var mockInterface = new Mock<IFakeInterface>();
        //     mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
        //         .Throws(new NullReferenceException());
        //     var mockInstance = mockInterface.Object;

        //     Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

        //     await Assert.ThrowsAsync<NullReferenceException>(action);
        //     mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        // }

        // [Theory]
        // [InlineData(HttpStatusCode.BadRequest)]
        // [InlineData(HttpStatusCode.Unauthorized)]
        // [InlineData(HttpStatusCode.Redirect)]
        // [InlineData(HttpStatusCode.SwitchingProtocols)]
        // public async Task AddWaitRetryPolicy_ReturnHttpNonTransientError_DoesNotRetry(HttpStatusCode transientStatusCode)
        // {
        //     var retryCount = 2;
        //     var policy = HttpClientPollyPolicy.Initialise()
        //         .AddWaitRetryPolicy(retryCount)
        //         .Build();
        //     var mockInterface = new Mock<IFakeInterface>();
        //     mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
        //         .Returns(Task.FromResult(new HttpResponseMessage(transientStatusCode)));
        //     var mockInstance = mockInterface.Object;

        //     var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

        //     Assert.Equal(transientStatusCode, result.StatusCode);
        //     mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        // }

        // [Theory]
        // [InlineData(HttpStatusCode.TooManyRequests)]
        // [InlineData(HttpStatusCode.RequestTimeout)]
        // [InlineData(HttpStatusCode.ServiceUnavailable)]
        // [InlineData(HttpStatusCode.InternalServerError)]
        // public async Task AddWaitRetryPolicy_ReturnHttpTransientError_RetryUptoCount(HttpStatusCode transientStatusCode)
        // {
        //     var retryCount = 2;
        //     var policy = HttpClientPollyPolicy.Initialise()
        //         .AddWaitRetryPolicy(retryCount)
        //         .Build();
        //     var mockInterface = new Mock<IFakeInterface>();
        //     mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
        //         .Returns(Task.FromResult(new HttpResponseMessage(transientStatusCode)));
        //     var mockInstance = mockInterface.Object;

        //     var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

        //     Assert.Equal(transientStatusCode, result.StatusCode);
        //     mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(retryCount + 1));
        // }

        // [Fact]
        // public async Task AddWaitRetryPolicy_ThrowHttpRequestException_RetryUptoCount()
        // {
        //     var retryCount = 2;
        //     var policy = HttpClientPollyPolicy.Initialise()
        //         .AddWaitRetryPolicy(retryCount)
        //         .Build();
        //     var mockInterface = new Mock<IFakeInterface>();
        //     mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
        //         .Throws(new HttpRequestException());
        //     var mockInstance = mockInterface.Object;

        //     Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

        //     await Assert.ThrowsAsync<HttpRequestException>(action);
        //     mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(retryCount + 1));
        // }

        // [Fact]
        // public async Task AddTimeoutPolicy_OperationCompleteWithinTimeoutPeriod_CompleteSuccessfully()
        // {
        //     var timeoutInMilliseconds = 1000;
        //     var policy = HttpClientPollyPolicy.Initialise()
        //         .AddTimeoutPolicy(timeoutInMilliseconds / 1000)
        //         .Build();
        //     var mockInterface = new Mock<IFakeInterface>();
        //     mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
        //         .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK), TimeSpan.FromMilliseconds(timeoutInMilliseconds / 2));
        //     var mockInstance = mockInterface.Object;

        //     var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

        //     Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        //     mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        // }

        // [Fact]
        // public async Task AddTimeoutPolicy_OperationExceedTimeoutPeriod_ThrowsPollyTimeoutRejectedException()
        // {
        //     var timeoutInMilliseconds = 1000;
        //     var policy = HttpClientPollyPolicy.Initialise()
        //         .AddTimeoutPolicy(timeoutInMilliseconds / 1000)
        //         .Build();
        //     var mockInterface = new Mock<IFakeInterface>();
        //     mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
        //         .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK), TimeSpan.FromMilliseconds(timeoutInMilliseconds + 20));
        //     var mockInstance = mockInterface.Object;

        //     Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

        //     await Assert.ThrowsAsync<TimeoutRejectedException>(action);
        //     mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        // }

        [Theory]
        [InlineData(typeof(HttpRequestException))]
        [InlineData(typeof(TimeoutRejectedException))]
        [InlineData(typeof(BrokenCircuitException))]
        public async Task AddFallbackPolicy_WithHandledExceptions_InvokeFallbackMethod(Type exceptionType)
        {
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Throws(Activator.CreateInstance(exceptionType) as Exception);
            mockInterface.Setup(exp => exp.FakeCallback())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddFallbackPolicy(() => mockInstance.FakeCallback())
                .Build();

            var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
            mockInterface.Verify(exp => exp.FakeCallback(), Times.Exactly(1));
        }

        [Theory]
        [InlineData(HttpStatusCode.RequestTimeout)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        [InlineData(HttpStatusCode.HttpVersionNotSupported)]
        [InlineData(HttpStatusCode.NotImplemented)]
        public async Task AddFallbackPolicy_WithHandledHttpErrors_InvokeFallbackMethod(HttpStatusCode httpStatusCode)
        {
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(new HttpResponseMessage(httpStatusCode)));
            mockInterface.Setup(exp => exp.FakeCallback())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddFallbackPolicy(() => mockInstance.FakeCallback())
                .Build();

            var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
            mockInterface.Verify(exp => exp.FakeCallback(), Times.Exactly(1));
        }

        [Theory]
        [InlineData(typeof(NullReferenceException))]
        [InlineData(typeof(ArgumentNullException))]
        public async Task AddFallbackPolicy_WithUnhandledExceptions_DoesNotInvokeFallbackMethod(Type exceptionType)
        {
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Throws(Activator.CreateInstance(exceptionType) as Exception);
            mockInterface.Setup(exp => exp.FakeCallback())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddFallbackPolicy(() => mockInstance.FakeCallback())
                .Build();

            Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            await Assert.ThrowsAsync(exceptionType, action);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
            mockInterface.Verify(exp => exp.FakeCallback(), Times.Never);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotModified)]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.PartialContent)]
        [InlineData(HttpStatusCode.PaymentRequired)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Redirect)]
        public async Task AddFallbackPolicy_WithUnhandledHttpErrors_DoesNotInvokeFallbackMethod(HttpStatusCode httpStatusCode)
        {
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(new HttpResponseMessage(httpStatusCode)));
            mockInterface.Setup(exp => exp.FakeCallback())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddFallbackPolicy(() => mockInstance.FakeCallback())
                .Build();

            var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(httpStatusCode, result.StatusCode);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
            mockInterface.Verify(exp => exp.FakeCallback(), Times.Never);
        }
    }
}