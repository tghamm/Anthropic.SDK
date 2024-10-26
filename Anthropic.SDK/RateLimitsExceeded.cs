using System;
using System.Net;
using System.Net.Http;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK;

/// <summary>
/// Thrown when the caller has exhausted current rate limits
/// The caller should wait until RetryAfter before making another request
/// </summary>
public class RateLimitsExceeded : HttpRequestException
{
    /// <summary>
    /// Rate limits as returned by the API
    /// </summary>
    public RateLimits RateLimits { get; }

    /// <inheritdoc />
    public RateLimitsExceeded(string message, RateLimits rateLimits, HttpStatusCode statusCode) :
#if NET6_0_OR_GREATER
        base(message, null, statusCode)
#else
        base(message)
#endif
    {
        RateLimits = rateLimits;
    }
}