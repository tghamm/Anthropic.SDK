using System;
using System.Net;
using System.Net.Http;
using Anthropic.SDK.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Anthropic.SDK.Extensions
{
    /// <summary>
    /// Extension methods for configuring AnthropicClient with HttpClientFactory and resilience patterns
    /// </summary>
    public static class AnthropicHttpClientExtensions
    {
        /// <summary>
        /// The name of the standard HttpClient (no resilience)
        /// </summary>
        public const string HttpClientName = "AnthropicClient";

        /// <summary>
        /// The name of the resilient HttpClient (with retry, circuit breaker, etc.)
        /// </summary>
        public const string ResilientHttpClientName = "AnthropicClient.Resilient";

        /// <summary>
        /// Adds a standard named HttpClient for AnthropicClient without resilience patterns.
        /// Use this for backward compatibility or when you want full manual control.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>An IHttpClientBuilder that can be used to configure the client further</returns>
        /// <example>
        /// // Register the client
        /// services.AddAnthropicClient();
        ///
        /// // Use it
        /// var httpClient = httpClientFactory.CreateClient(AnthropicHttpClientExtensions.HttpClientName);
        /// var client = new AnthropicClient(apiAuth, httpClient);
        /// </example>
        public static IHttpClientBuilder AddAnthropicClient(this IServiceCollection services)
        {
            return services.AddHttpClient(HttpClientName)
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                });
        }

        /// <summary>
        /// Adds a resilient named HttpClient for AnthropicClient with retry, circuit breaker, and timeout patterns.
        /// Recommended for production use.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureResilience">Optional action to configure resilience options. If not provided, uses sensible defaults.</param>
        /// <returns>An IHttpClientBuilder that can be used to configure the client further</returns>
        /// <example>
        /// // Use default resilience (3 retries, exponential backoff, jitter)
        /// services.AddAnthropicClientWithResilience();
        ///
        /// // Customize resilience
        /// services.AddAnthropicClientWithResilience(options =>
        /// {
        ///     options.MaxRetryAttempts = 5;
        ///     options.EnableCircuitBreaker = true;
        ///     options.OnRetry = (attempt, delay, ex) =>
        ///         Console.WriteLine($"Retry {attempt} after {delay}");
        /// });
        ///
        /// // Use it
        /// var httpClient = httpClientFactory.CreateClient(AnthropicHttpClientExtensions.ResilientHttpClientName);
        /// var client = new AnthropicClient(apiAuth, httpClient);
        /// </example>
        public static IHttpClientBuilder AddAnthropicClientWithResilience(
            this IServiceCollection services,
            Action<ResilienceOptions>? configureResilience = null)
        {
            var options = ResilienceOptions.Default;
            configureResilience?.Invoke(options);

            var builder = services.AddHttpClient(ResilientHttpClientName)
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = options.Timeout ?? TimeSpan.FromMinutes(10);
                });

            // Build resilience pipeline
            builder.AddResilienceHandler("anthropic-resilience", (resilienceBuilder, context) =>
            {
                // Retry policy (always added for resilient client)
                if (options.EnableRetry && options.MaxRetryAttempts > 0)
                {
                    resilienceBuilder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = options.MaxRetryAttempts,
                        Delay = options.BaseDelay,
                        BackoffType = options.UseExponentialBackoff
                            ? DelayBackoffType.Exponential
                            : DelayBackoffType.Constant,
                        UseJitter = options.UseJitter,
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutException>()
                            .HandleResult(response =>
                            {
                                var statusCode = (int)response.StatusCode;
                                return statusCode == 429 ||  // Rate limit
                                       statusCode == 500 ||  // Internal server error
                                       statusCode == 502 ||  // Bad gateway
                                       statusCode == 503 ||  // Service unavailable
                                       statusCode == 504;    // Gateway timeout
                            }),
                        OnRetry = args =>
                        {
                            // Respect Retry-After header for 429 responses
                            if (args.Outcome.Result?.StatusCode == (HttpStatusCode)429)
                            {
                                var retryAfter = args.Outcome.Result.Headers.RetryAfter?.Delta;
                                if (retryAfter.HasValue && retryAfter.Value > args.RetryDelay)
                                {
                                    args.RetryDelay = retryAfter.Value;
                                }
                            }

                            // Invoke user callback if provided
                            options.OnRetry?.Invoke(
                                args.AttemptNumber,
                                args.RetryDelay,
                                args.Outcome.Exception);

                            return default;
                        }
                    });
                }

                // Circuit breaker (optional)
                if (options.EnableCircuitBreaker)
                {
                    resilienceBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = 0.5,
                        MinimumThroughput = options.CircuitBreakerFailureThreshold,
                        BreakDuration = options.CircuitBreakerBreakDuration,
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .HandleResult(response => (int)response.StatusCode >= 500)
                    });
                }

                // Timeout (optional, separate from HttpClient timeout)
                if (options.Timeout.HasValue)
                {
                    resilienceBuilder.AddTimeout(options.Timeout.Value);
                }
            });

            return builder;
        }
    }
}
