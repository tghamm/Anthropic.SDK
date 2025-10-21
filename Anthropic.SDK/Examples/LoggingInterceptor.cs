using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Anthropic.SDK.Examples
{
    /// <summary>
    /// Example implementation of IRequestInterceptor that logs HTTP request and response details.
    /// This is a reference implementation showing how to add logging, metrics, and diagnostics.
    /// </summary>
    /// <remarks>
    /// This interceptor logs request/response information for debugging and monitoring purposes.
    /// It measures request duration and captures status codes, URLs, and optional request/response bodies.
    ///
    /// Usage:
    /// <code>
    /// var loggingInterceptor = new LoggingInterceptor(
    ///     logRequestBody: true,
    ///     logResponseBody: true
    /// );
    ///
    /// var client = new AnthropicClient(
    ///     apiKeys: new APIAuthentication("your-api-key"),
    ///     requestInterceptor: loggingInterceptor
    /// );
    /// </code>
    /// </remarks>
    public class LoggingInterceptor : IRequestInterceptor
    {
        private readonly bool _logRequestBody;
        private readonly bool _logResponseBody;

        /// <summary>
        /// Creates a new LoggingInterceptor with the specified logging options.
        /// </summary>
        /// <param name="logRequestBody">Whether to log request body content (default: false)</param>
        /// <param name="logResponseBody">Whether to log response body content (default: false)</param>
        public LoggingInterceptor(
            bool logRequestBody = false,
            bool logResponseBody = false)
        {
            _logRequestBody = logRequestBody;
            _logResponseBody = logResponseBody;
        }

        /// <summary>
        /// Intercepts the HTTP request and logs request/response details.
        /// </summary>
        public async Task<HttpResponseMessage> InvokeAsync(
            HttpRequestMessage request,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next,
            CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Log request
                await LogRequestAsync(requestId, request, cancellationToken).ConfigureAwait(false);

                // Execute the request
                var response = await next(request, cancellationToken).ConfigureAwait(false);

                stopwatch.Stop();

                // Log response
                await LogResponseAsync(requestId, response, stopwatch.ElapsedMilliseconds, cancellationToken).ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogException(requestId, ex, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Logs HTTP request details.
        /// </summary>
        private async Task LogRequestAsync(string requestId, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var logMessage = $"[{requestId}] → {request.Method} {request.RequestUri}";

            if (_logRequestBody && request.Content != null)
            {
                try
                {
                    // Buffer content to memory so it can be read multiple times
                    await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);

                    var contentLength = request.Content.Headers.ContentLength ?? 0;

                    // Only log bodies under 10KB to avoid performance issues
                    if (contentLength > 0 && contentLength < 10_000)
                    {
                        var content = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                        logMessage += Environment.NewLine + $"Request Body ({contentLength} bytes): {content}";
                    }
                    else if (contentLength == 0)
                    {
                        logMessage += Environment.NewLine + "Request Body: [Empty]";
                    }
                    else if (contentLength >= 10_000)
                    {
                        logMessage += Environment.NewLine + $"Request Body: [Too large to log - {contentLength} bytes]";
                    }
                }
                catch (Exception ex)
                {
                    logMessage += Environment.NewLine + $"Request Body: [Failed to read - {ex.Message}]";
                }
            }

            LogRequest(requestId, request.Method.Method, request.RequestUri?.ToString(), logMessage);
        }

        /// <summary>
        /// Logs HTTP response details.
        /// </summary>
        private async Task LogResponseAsync(
            string requestId,
            HttpResponseMessage response,
            long elapsedMs,
            CancellationToken cancellationToken)
        {
            var logMessage = $"[{requestId}] ← {(int)response.StatusCode} {response.ReasonPhrase} ({elapsedMs}ms)";

            if (_logResponseBody && response.Content != null)
            {
                try
                {
                    // Buffer content to memory so it can be read by both logger and caller
                    await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);

                    var contentLength = response.Content.Headers.ContentLength ?? 0;

                    // Only log bodies under 10KB
                    if (contentLength > 0 && contentLength < 10_000)
                    {
#if NET6_0_OR_GREATER
                        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                        logMessage += Environment.NewLine + $"Response Body ({contentLength} bytes): {content}";
                    }
                    else if (contentLength == 0)
                    {
                        logMessage += Environment.NewLine + "Response Body: [Empty]";
                    }
                    else if (contentLength >= 10_000)
                    {
                        logMessage += Environment.NewLine + $"Response Body: [Too large to log - {contentLength} bytes]";
                    }
                }
                catch (Exception ex)
                {
                    // Don't fail the request if logging fails
                    logMessage += Environment.NewLine + $"Response Body: [Failed to read - {ex.Message}]";
                }
            }

            LogResponse(requestId, (int)response.StatusCode, response.ReasonPhrase, elapsedMs, logMessage);
        }

        /// <summary>
        /// Logs exceptions that occur during request execution.
        /// Override this method to implement custom logging.
        /// </summary>
        protected virtual void LogException(string requestId, Exception exception, long elapsedMs)
        {
            // Default: no logging. Override in derived class for custom logging.
            // Example: Console.WriteLine($"[{requestId}] ✗ Exception after {elapsedMs}ms: {exception.Message}");
        }

        /// <summary>
        /// Logs outgoing HTTP request.
        /// Override this method to implement custom logging.
        /// </summary>
        protected virtual void LogRequest(string requestId, string method, string url, string fullMessage)
        {
            // Default: no logging. Override in derived class for custom logging.
            // Example: Console.WriteLine(fullMessage);
        }

        /// <summary>
        /// Logs incoming HTTP response.
        /// Override this method to implement custom logging.
        /// </summary>
        protected virtual void LogResponse(string requestId, int statusCode, string reasonPhrase, long elapsedMs, string fullMessage)
        {
            // Default: no logging. Override in derived class for custom logging.
            // Example: Console.WriteLine(fullMessage);
        }
    }
}
