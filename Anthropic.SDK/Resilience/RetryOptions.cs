using System;

namespace Anthropic.SDK.Resilience
{
    /// <summary>
    /// Configuration options for retry behavior
    /// </summary>
    public class RetryOptions
    {
        /// <summary>
        /// Maximum number of retry attempts. Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay between retry attempts. Default is 1 second.
        /// </summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum delay between retry attempts. Default is 60 seconds.
        /// Prevents exponential backoff from growing too large.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Whether to use exponential backoff. Default is true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Whether to add jitter to retry delays. Default is true.
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Optional callback invoked before each retry attempt.
        /// Parameters: (attemptNumber, delay, exception)
        /// </summary>
        public Action<int, TimeSpan, Exception?>? OnRetry { get; set; }

        /// <summary>
        /// Default retry options with recommended settings
        /// </summary>
        public static RetryOptions Default => new RetryOptions();
    }
}
