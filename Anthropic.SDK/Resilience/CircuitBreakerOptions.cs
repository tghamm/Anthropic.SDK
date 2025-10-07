using System;

namespace Anthropic.SDK.Resilience
{
    /// <summary>
    /// Configuration options for circuit breaker pattern
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Number of consecutive failures before the circuit breaker opens. Default is 5.
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duration the circuit breaker stays open before attempting to close. Default is 30 seconds.
        /// </summary>
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Optional callback invoked when the circuit breaker opens.
        /// Parameter: exception that caused the break
        /// </summary>
        public Action<Exception>? OnBreak { get; set; }

        /// <summary>
        /// Optional callback invoked when the circuit breaker resets.
        /// </summary>
        public Action? OnReset { get; set; }

        /// <summary>
        /// Default circuit breaker options with recommended settings
        /// </summary>
        public static CircuitBreakerOptions Default => new CircuitBreakerOptions();
    }
}
