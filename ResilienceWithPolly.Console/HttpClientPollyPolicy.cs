
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ResilienceWithPolly.Console
{
    public class HttpClientPollyPolicy
    {
        private readonly Dictionary<string, IAsyncPolicy<HttpResponseMessage>> _policies;
        private static readonly string RetryKey = $"{nameof(HttpClientPollyPolicy)}_{nameof(RetryKey)}";
        private static readonly string TimeoutKey = $"{nameof(HttpClientPollyPolicy)}_{nameof(TimeoutKey)}";
        private static readonly string CircuitBreakerKey = $"{nameof(HttpClientPollyPolicy)}_{nameof(CircuitBreakerKey)}";
        private static readonly string FallbackKey = $"{nameof(HttpClientPollyPolicy)}_{nameof(FallbackKey)}";
        private static readonly string HttpClientPollyPolicyKey = $"{nameof(HttpClientPollyPolicy)}_{nameof(HttpClientPollyPolicyKey)}";

        public IReadOnlyDictionary<string, IAsyncPolicy<HttpResponseMessage>> Policies => _policies;

        private HttpClientPollyPolicy()
        {
            _policies = new Dictionary<string, IAsyncPolicy<HttpResponseMessage>>();
        }

        public static HttpClientPollyPolicy Initialise()
        {
            return new HttpClientPollyPolicy();
        }

        public IAsyncPolicy<HttpResponseMessage> Build()
        {
            if (_policies == null || !_policies.Any())
            {
                throw new ArgumentException("No policies have been defined");
            }

            var policies = _policies.OrderByDescending(p => GetPolicyPriority(p.Key)).ToList();
            var policy = policies.First().Value;

            if (policies.Count > 1)
            {
                for (int i = 1; i < policies.Count; i++)
                {
                    policy.WrapAsync(policies[i].Value);
                }
            }
            return policy.WithPolicyKey(HttpClientPollyPolicyKey);
        }

        public HttpClientPollyPolicy AddWaitRetryPolicy(int retryCount)
        {
            if (!_policies.ContainsKey(RetryKey))
            {
                var policy = Policy.Handle<HttpRequestException>()
                .OrResult(TransientHttpStatusCodePredicate)
                .WaitAndRetryAsync(retryCount, ExponentialBackoffTimespan)
                .WithPolicyKey(RetryKey);

                _policies.Add(RetryKey, policy);
            }

            return this;
        }

        public HttpClientPollyPolicy AddTimeoutPolicy(int timeoutInSeconds)
        {
            if (!_policies.ContainsKey(TimeoutKey))
            {
                var policy = Policy.TimeoutAsync<HttpResponseMessage>(timeoutInSeconds, TimeoutStrategy.Pessimistic)
                .WithPolicyKey(TimeoutKey);

                _policies.Add(TimeoutKey, policy);
            }

            return this;
        }

        public HttpClientPollyPolicy AddCircuitBreakerPolicy(int exceptionAllowedBeforeBreaking, TimeSpan durationOfBreak)
        {
            if (!_policies.ContainsKey(CircuitBreakerKey))
            {
                var policy1 = Policy.Handle<HttpRequestException>()
                    .CircuitBreakerAsync(exceptionAllowedBeforeBreaking, durationOfBreak);

                var policy2 = Policy.HandleResult<HttpResponseMessage>(httpResponseMessage => (int)httpResponseMessage.StatusCode >= 500)
                    .CircuitBreakerAsync(exceptionAllowedBeforeBreaking, durationOfBreak);

                var policy3 = Policy.HandleResult<HttpResponseMessage>(httpResponseMessage => httpResponseMessage.StatusCode == HttpStatusCode.RequestTimeout)
                    .CircuitBreakerAsync(exceptionAllowedBeforeBreaking, durationOfBreak);

                var policy4 = Policy.HandleResult<HttpResponseMessage>(httpResponseMessage => httpResponseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                    .CircuitBreakerAsync(exceptionAllowedBeforeBreaking, durationOfBreak);

                var policy = policy1.WrapAsync(policy2).WrapAsync(policy3).WrapAsync(policy4).WithPolicyKey(CircuitBreakerKey);

                _policies.Add(CircuitBreakerKey, policy);
            }

            return this;
        }

        public HttpClientPollyPolicy AddFallbackPolicy(Func<Task<HttpResponseMessage>> action)
        {
            if (!_policies.ContainsKey(FallbackKey))
            {
                var policy = Policy.Handle<HttpRequestException>()
                    .OrResult(TransientHttpStatusCodePredicate)
                    .Or<TimeoutRejectedException>()
                    .Or<BrokenCircuitException>()
                    .FallbackAsync(cancellationToken => action())
                    .WithPolicyKey(FallbackKey);

                _policies.Add(FallbackKey, policy);
            }

            return this;
        }

        private readonly Func<HttpResponseMessage, bool> TransientHttpStatusCodePredicate = (response) =>
        {
            return (int)response.StatusCode >= 500
                || response.StatusCode == HttpStatusCode.RequestTimeout
                || response.StatusCode == HttpStatusCode.TooManyRequests;
        };

        private Func<int, TimeSpan> ExponentialBackoffTimespan = retryNumber =>
        {
            return TimeSpan.FromSeconds(Math.Pow(2, retryNumber));
        };

        private int GetPolicyPriority(string policyKey)
        {
            if (policyKey.Equals(FallbackKey))
            {
                return 10000;
            }

            if (policyKey.Equals(TimeoutKey))
            {
                return 1000;
            }

            return 10;
        }
    }
}