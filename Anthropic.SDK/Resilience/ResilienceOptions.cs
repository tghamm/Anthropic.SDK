using System;
using Microsoft.Extensions.Http.Resilience;

namespace Anthropic.SDK.Resilience
{
    /// <summary>
    /// Configuration options for resilience patterns (retry, circuit breaker, timeout) in API requests.
    /// Uses Microsoft.Extensions.Http.Resilience for standard resilience patterns.
    /// </summary>
    public class ResilienceOptions
    {
        /// <summary>
        /// Whether to enable retry logic. Default is true.
        /// When enabled, transient failures (429, 500, 502, 503, 504) will be automatically retried.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts. Default is 3.
        /// Set to 0 to disable retries even when EnableRetry is true.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay between retry attempts. Default is 1 second.
        /// When using exponential backoff, actual delay will be: BaseDelay * (2 ^ attempt).
        /// </summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Whether to use exponential backoff for retry delays. Default is true.
        /// When true: delay grows exponentially (1s, 2s, 4s, 8s, etc.)
        /// When false: delay remains constant at BaseDelay
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Whether to add random jitter to retry delays. Default is true.
        /// Jitter helps prevent thundering herd problems when many clients retry simultaneously.
        /// Adds random variation of ±25% to the calculated delay.
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Whether to enable circuit breaker pattern. Default is false.
        /// Circuit breaker prevents cascading failures by temporarily blocking requests
        /// when too many failures occur.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = false;

        /// <summary>
        /// Number of consecutive failures before the circuit breaker opens. Default is 5.
        /// Only applies when EnableCircuitBreaker is true.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duration the circuit breaker stays open before attempting to close. Default is 30 seconds.
        /// Only applies when EnableCircuitBreaker is true.
        /// </summary>
        public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Overall timeout for HTTP requests. Default is null (no timeout override).
        /// When set, requests exceeding this duration will be cancelled.
        /// Note: Anthropic API has its own timeouts; this is an additional client-side timeout.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Optional callback invoked before each retry attempt.
        /// Useful for logging or custom retry logic.
        /// Parameters: (attemptNumber, delay, exception)
        /// </summary>
        public Action<int, TimeSpan, Exception>? OnRetry { get; set; }

        /// <summary>
        /// Creates default resilience options with recommended settings for Anthropic API.
        /// - Retry enabled with 3 attempts
        /// - Exponential backoff with jitter
        /// - No circuit breaker (API has rate limiting)
        /// </summary>
        public static ResilienceOptions Default => new ResilienceOptions();

        /// <summary>
        /// Creates resilience options with all resilience features disabled.
        /// Use this for debugging or when you want to handle failures manually.
        /// </summary>
        public static ResilienceOptions None => new ResilienceOptions
        {
            EnableRetry = false,
            EnableCircuitBreaker = false
        };
    }
}
