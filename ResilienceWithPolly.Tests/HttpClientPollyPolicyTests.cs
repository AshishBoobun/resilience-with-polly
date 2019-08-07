using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
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

        [Fact]
        public async Task AddTimeoutPolicy_OperationCompleteWithinTimeoutPeriod_CompleteSuccessfully()
        {
            var timeoutInMilliseconds = 1000;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddTimeoutPolicy(timeoutInMilliseconds / 1000)
                .Build();
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK), TimeSpan.FromMilliseconds(timeoutInMilliseconds / 2));
            var mockInstance = mockInterface.Object;

            var result = await policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        }

        [Fact]
        public async Task AddTimeoutPolicy_OperationExceedTimeoutPeriod_ThrowsPollyTimeoutRejectedException()
        {
            var timeoutInMilliseconds = 1000;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddTimeoutPolicy(timeoutInMilliseconds / 1000)
                .Build();
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK), TimeSpan.FromMilliseconds(timeoutInMilliseconds + 20));
            var mockInstance = mockInterface.Object;

            Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            await Assert.ThrowsAsync<TimeoutRejectedException>(action);
            mockInterface.Verify(exp => exp.DoSomethingAsync(It.IsAny<int>()), Times.Exactly(1));
        }

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

        [Fact]
        public async Task AddCircuitBreakerPolicy_HavingRepeatedHttpRequestException_ActivateCircuitBreak()
        {
            var exceptionAllowedBeforeBreaking = 2;
            var circuitBreakDuration = 400;
            var message = "sample exception";
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(1))
                .Throws(new HttpRequestException(message));
            mockInterface.Setup(exp => exp.DoSomethingAsync(2))
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddCircuitBreakerPolicy(exceptionAllowedBeforeBreaking, TimeSpan.FromMilliseconds(circuitBreakDuration))
                .Build();

            Func<int, Task<HttpResponseMessage>> action = count => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(count));

            await Assert.ThrowsAsync<HttpRequestException>(() => action(1));
            await Assert.ThrowsAsync<HttpRequestException>(() => action(1));
            await Assert.ThrowsAsync<BrokenCircuitException>(() => action(1));
            await Task.Delay(circuitBreakDuration);

            //Half Open goes to Close
            var result = await action(2);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            await Assert.ThrowsAsync<HttpRequestException>(() => action(1));
        }


        [Theory]
        [InlineData(HttpStatusCode.RequestTimeout)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        [InlineData(HttpStatusCode.HttpVersionNotSupported)]
        [InlineData(HttpStatusCode.NotImplemented)]
        public async Task AddCircuitBreakerPolicy_HavingRepeatedHandledHttpErrors_ActivateCircuitBreak(HttpStatusCode httpStatusCode)
        {
            var exceptionAllowedBeforeBreaking = 2;
            var circuitBreakDuration = 400;
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(new HttpResponseMessage(httpStatusCode)));
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddCircuitBreakerPolicy(exceptionAllowedBeforeBreaking, TimeSpan.FromMilliseconds(circuitBreakDuration))
                .Build();

            Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(httpStatusCode, (await action()).StatusCode);
            Assert.Equal(httpStatusCode, (await action()).StatusCode);

            await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(action);
            await Task.Delay(circuitBreakDuration);
            Assert.Equal(httpStatusCode, (await action()).StatusCode);
        }

        [Theory]
        [InlineData(typeof(NullReferenceException))]
        [InlineData(typeof(ArgumentNullException))]
        public async Task AddCircuitBreakerPolicy_HavingRepeatedUnhandledException_DoesNotActivateCircuitBreak(Type exceptionType)
        {
            var exceptionAllowedBeforeBreaking = 2;
            var circuitBreakDuration = 400;
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Throws(Activator.CreateInstance(exceptionType) as Exception);
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddCircuitBreakerPolicy(exceptionAllowedBeforeBreaking, TimeSpan.FromMilliseconds(circuitBreakDuration))
                .Build();

            Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            await Assert.ThrowsAsync(exceptionType, action);
            await Assert.ThrowsAsync(exceptionType, action);
            await Assert.ThrowsAsync(exceptionType, action);
            await Assert.ThrowsAsync(exceptionType, action);
            await Task.Delay(circuitBreakDuration);
            await Assert.ThrowsAsync(exceptionType, action);
        }


        [Theory]
        [InlineData(HttpStatusCode.NotModified)]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.PartialContent)]
        [InlineData(HttpStatusCode.PaymentRequired)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Redirect)]
        public async Task AddCircuitBreakerPolicy_HavingRepeatedUnHandledHttpErrors_DoesNotActivateCircuitBreak(HttpStatusCode httpStatusCode)
        {
            var exceptionAllowedBeforeBreaking = 2;
            var circuitBreakDuration = 400;
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(new HttpResponseMessage(httpStatusCode)));
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddCircuitBreakerPolicy(exceptionAllowedBeforeBreaking, TimeSpan.FromMilliseconds(circuitBreakDuration))
                .Build();

            Func<Task<HttpResponseMessage>> action = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(httpStatusCode, (await action()).StatusCode);
            Assert.Equal(httpStatusCode, (await action()).StatusCode);
            Assert.Equal(httpStatusCode, (await action()).StatusCode);
            await Task.Delay(circuitBreakDuration);
            Assert.Equal(httpStatusCode, (await action()).StatusCode);
        }

        [Fact]
        public async Task AddCircuitBreakerPolicy_RandomHandledHttpErrors_OnlyBreakWhenConsecutiveHandledErrorsDetected()
        {
            var statusCodeInOrder = new[] { HttpStatusCode.RequestTimeout, HttpStatusCode.RequestTimeout, HttpStatusCode.TooManyRequests, 
                                            HttpStatusCode.RequestTimeout, HttpStatusCode.RequestTimeout, HttpStatusCode.RequestTimeout, HttpStatusCode.RequestTimeout };
            var exceptionAllowedBeforeBreaking = 3;
            var circuitBreakDuration = 400;
            var mockInterface = new Mock<IFakeInterface>();
            mockInterface.Setup(exp => exp.DoSomethingAsync(It.IsAny<int>()))
                .Returns((int count) => Task.FromResult(new HttpResponseMessage(statusCodeInOrder[count])));
            var mockInstance = mockInterface.Object;
            var policy = HttpClientPollyPolicy.Initialise()
                .AddCircuitBreakerPolicy(exceptionAllowedBeforeBreaking, TimeSpan.FromMilliseconds(circuitBreakDuration))
                .Build();
            
            Func<int, Task<HttpResponseMessage>> action = (int index) => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(index));
            Func<Task<HttpResponseMessage>> actionNoParam = () => policy.ExecuteAsync(() => mockInstance.DoSomethingAsync(3));

            Assert.Equal(statusCodeInOrder[0], (await action(0)).StatusCode);
            Assert.Equal(statusCodeInOrder[1], (await action(1)).StatusCode);
            Assert.Equal(statusCodeInOrder[2], (await action(2)).StatusCode);
            Assert.Equal(statusCodeInOrder[3], (await action(3)).StatusCode);
            Assert.Equal(statusCodeInOrder[4], (await action(4)).StatusCode);
            Assert.Equal(statusCodeInOrder[5], (await action(5)).StatusCode);

            await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(actionNoParam);
            await Task.Delay(circuitBreakDuration);
            Assert.Equal(statusCodeInOrder[6], (await action(6)).StatusCode);
        }

        [Fact]
        public async Task HttpClientPolicy_CombinePolicies_BehavesAccordingToPolicy()
        {
            var retryCount = 3;
            var exceptionAllowedBeforeBreaking = retryCount + 2;
            var circuitBreakDurationInMs = 10000;
            var timeoutInSeconds = 25;

            var foo = new FooStub();

            var policy = HttpClientPollyPolicy.Initialise()
                .AddWaitRetryPolicy(retryCount)
                .AddFallbackPolicy(() => foo.FakeCallback())
                .AddCircuitBreakerPolicy(exceptionAllowedBeforeBreaking, TimeSpan.FromMilliseconds(circuitBreakDurationInMs))
                .AddTimeoutPolicy(timeoutInSeconds)
                .Build();

            Func<bool, int?, Task<HttpResponseMessage>> barAsync = (bool returnFailureMessage, int? fakeTimeoutInSeconds) => policy.ExecuteAsync(() => foo.BarAsync(returnFailureMessage, fakeTimeoutInSeconds));

            var result = await barAsync(true, null);
            int barAsyncMethodInvocationCount = retryCount + 1;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            result = await barAsync(true, null);
            barAsyncMethodInvocationCount++;
            int callbackMethodInvocationCount = 3;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            await Task.Delay(circuitBreakDurationInMs);

            //Return unhandled error code to transition circuit from half-open to close
            result = await barAsync(false, null);
            barAsyncMethodInvocationCount++;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            result = await barAsync(true, null);
            barAsyncMethodInvocationCount += retryCount + 1;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            result = await barAsync(true, timeoutInSeconds);
            barAsyncMethodInvocationCount++;
            callbackMethodInvocationCount++;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            Assert.Equal(callbackMethodInvocationCount, foo.FallbackCount);
            Assert.Equal(barAsyncMethodInvocationCount, foo.BarAsyncCount);
        }

        public class FooStub
        {
            public int BarAsyncCount { get; private set; }

            public int FallbackCount { get; private set; }

            public FooStub()
            {
                BarAsyncCount = 0;
                FallbackCount = 0;
            }

            public async Task<HttpResponseMessage> BarAsync(bool returnFailureMessage, int? fakeTimeoutInSeconds = null)
            {
                BarAsyncCount++;

                if (fakeTimeoutInSeconds.HasValue && fakeTimeoutInSeconds.Value > 0)
                {
                    await Task.Delay(fakeTimeoutInSeconds.Value * 1000);
                }

                if (returnFailureMessage)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            public Task<HttpResponseMessage> FakeCallback()
            {
                FallbackCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }
        }
    }
}