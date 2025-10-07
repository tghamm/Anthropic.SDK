using System;

namespace Anthropic.SDK.Resilience
{
    /// <summary>
    /// Configuration options for timeout behavior
    /// </summary>
    public class TimeoutOptions
    {
        /// <summary>
        /// Timeout duration for requests. Default is 5 minutes.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Optional callback invoked when a timeout occurs.
        /// Parameter: timeout duration
        /// </summary>
        public Action<TimeSpan>? OnTimeout { get; set; }

        /// <summary>
        /// Default timeout options with recommended settings
        /// </summary>
        public static TimeoutOptions Default => new TimeoutOptions();
    }
}
