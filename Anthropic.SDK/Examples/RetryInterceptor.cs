using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Anthropic.SDK.Examples
{
    /// <summary>
    /// Example implementation of IRequestInterceptor that adds retry logic with exponential backoff.
    /// This is a reference implementation showing how to handle transient failures.
    /// </summary>
    /// <remarks>
    /// This interceptor retries failed requests based on HTTP status codes and exceptions.
    /// It uses exponential backoff to avoid overwhelming the server during outages.
    ///
    /// This interceptor clones HttpRequestMessage for each retry attempt.
    /// Request content is buffered in memory before the first attempt,
    /// so this may not be suitable for very large request bodies (>100MB).
    /// For large uploads, consider implementing retries at the application level
    /// or using a streaming-friendly approach.
    ///
    /// Usage:
    /// <code>
    /// var retryInterceptor = new RetryInterceptor(
    ///     maxRetries: 3,
    ///     initialDelay: TimeSpan.FromSeconds(1)
    /// );
    ///
    /// var client = new AnthropicClient(
    ///     apiKeys: new APIAuthentication("your-api-key"),
    ///     requestInterceptor: retryInterceptor
    /// );
    /// </code>
    /// </remarks>
    public class RetryInterceptor : IRequestInterceptor
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly double _backoffMultiplier;

        /// <summary>
        /// Creates a new RetryInterceptor with the specified retry configuration.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
        /// <param name="initialDelay">Initial delay before first retry (default: 1 second)</param>
        /// <param name="backoffMultiplier">Multiplier for exponential backoff (default: 2.0)</param>
        public RetryInterceptor(
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            double backoffMultiplier = 2.0)
        {
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative");

            if (backoffMultiplier < 1.0)
                throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "Backoff multiplier must be >= 1.0");

            _maxRetries = maxRetries;
            _initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
            _backoffMultiplier = backoffMultiplier;
        }

        /// <summary>
        /// Intercepts the HTTP request and adds retry logic with exponential backoff.
        /// </summary>
        public async Task<HttpResponseMessage> InvokeAsync(
            HttpRequestMessage request,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next,
            CancellationToken cancellationToken = default)
        {
            var attempt = 0;
            Exception lastException = null;

            // CAPTURE CONTENT ONCE BEFORE ANY ATTEMPTS
            byte[] requestContent = null;
            if (request.Content != null)
            {
#if NET6_0_OR_GREATER
                requestContent = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                requestContent = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
            }

            while (attempt <= _maxRetries)
            {
                try
                {
                    // Clone for EVERY attempt (including first) using pre-captured content
                    var requestToSend = CloneRequest(request, requestContent);

                    var response = await next(requestToSend, cancellationToken).ConfigureAwait(false);

                    // Check if we should retry based on status code
                    if (ShouldRetry(response.StatusCode, attempt))
                    {
                        // Will dispose after this block
                        using (response)
                        {
                            // Capture diagnostics here
#if NET6_0_OR_GREATER
                            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                            LogRetryAttempt(attempt, response.StatusCode, errorBody);
                        }
                        // response is now disposed

                        await DelayBeforeRetry(attempt, cancellationToken).ConfigureAwait(false);
                        attempt++;
                        continue;
                    }

                    // DON'T dispose - return to caller
                    return response;
                }
                catch (Exception ex) when (IsTransientException(ex, cancellationToken) && attempt < _maxRetries)
                {
                    lastException = ex;
                    LogExceptionRetry(attempt, ex);
                    await DelayBeforeRetry(attempt, cancellationToken).ConfigureAwait(false);
                    attempt++;
                }
            }

            // Max retries exceeded
            if (lastException != null)
            {
                throw new HttpRequestException(
                    $"Request failed after {_maxRetries} retry attempts. See inner exception for details.",
                    lastException);
            }

            throw new HttpRequestException($"Request failed after {_maxRetries} retry attempts.");
        }

        /// <summary>
        /// Determines if a status code should trigger a retry.
        /// </summary>
        private bool ShouldRetry(HttpStatusCode statusCode, int currentAttempt)
        {
            if (currentAttempt >= _maxRetries)
                return false;

            // Retry on specific status codes
            return statusCode == HttpStatusCode.RequestTimeout ||           // 408
                   statusCode == (HttpStatusCode)429 ||                     // 429 TooManyRequests (not in netstandard2.0)
                   statusCode == HttpStatusCode.InternalServerError ||      // 500
                   statusCode == HttpStatusCode.BadGateway ||               // 502
                   statusCode == HttpStatusCode.ServiceUnavailable ||       // 503
                   statusCode == HttpStatusCode.GatewayTimeout;             // 504
        }

        /// <summary>
        /// Determines if an exception is transient and should trigger a retry.
        /// </summary>
        private bool IsTransientException(Exception ex, CancellationToken cancellationToken)
        {
            // Network-level failures that are typically transient
            return ex is HttpRequestException ||
                   ex is TimeoutException ||
                   // Only retry TaskCanceledException if it's NOT user-initiated cancellation
                   (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested);
        }

        /// <summary>
        /// Calculates and applies the delay before the next retry using exponential backoff.
        /// </summary>
        private async Task DelayBeforeRetry(int attemptNumber, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMilliseconds(
                _initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attemptNumber)
            );

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Logs retry attempts due to HTTP status codes.
        /// Override this method to implement custom logging.
        /// </summary>
        protected virtual void LogRetryAttempt(int attempt, HttpStatusCode statusCode, string errorBody)
        {
            // Default: no logging. Override in derived class for custom logging.
            // Example: Console.WriteLine($"Retry attempt {attempt + 1}/{_maxRetries} - Status: {statusCode}");
        }

        /// <summary>
        /// Logs retry attempts due to exceptions.
        /// Override this method to implement custom logging.
        /// </summary>
        protected virtual void LogExceptionRetry(int attempt, Exception exception)
        {
            // Default: no logging. Override in derived class for custom logging.
            // Example: Console.WriteLine($"Retry attempt {attempt + 1}/{_maxRetries} - Exception: {exception.Message}");
        }

        /// <summary>
        /// Clones an HttpRequestMessage using pre-captured content.
        /// </summary>
        /// <remarks>
        /// This method creates a new request with the same properties as the original,
        /// using content that was buffered before the first attempt.
        /// This ensures that request content can be replayed for retry attempts.
        /// </remarks>
        private HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[] contentBytes)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            // Copy headers
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Use pre-captured content
            if (contentBytes != null && contentBytes.Length > 0)
            {
                clone.Content = new ByteArrayContent(contentBytes);

                // Copy content headers from original request
                if (request.Content != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            return clone;
        }
    }
}
