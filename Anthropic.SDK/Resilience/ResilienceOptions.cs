using System;

namespace Anthropic.SDK.Resilience
{
    /// <summary>
    /// Aggregated resilience configuration options.
    /// Provides a convenience wrapper for configuring all resilience patterns together.
    /// </summary>
    public class ResilienceOptions
    {
        /// <summary>
        /// Whether to enable retry logic. Default is true.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Retry configuration options.
        /// </summary>
        public RetryOptions Retry { get; set; } = RetryOptions.Default;

        /// <summary>
        /// Whether to enable circuit breaker. Default is false.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = false;

        /// <summary>
        /// Circuit breaker configuration options.
        /// </summary>
        public CircuitBreakerOptions CircuitBreaker { get; set; } = CircuitBreakerOptions.Default;

        /// <summary>
        /// Timeout configuration options.
        /// </summary>
        public TimeoutOptions Timeout { get; set; } = TimeoutOptions.Default;

        /// <summary>
        /// Default resilience options with recommended settings for Anthropic API.
        /// </summary>
        public static ResilienceOptions Default => new ResilienceOptions();

        /// <summary>
        /// Resilience options with all features disabled.
        /// </summary>
        public static ResilienceOptions None => new ResilienceOptions
        {
            EnableRetry = false,
            EnableCircuitBreaker = false
        };
    }
}
