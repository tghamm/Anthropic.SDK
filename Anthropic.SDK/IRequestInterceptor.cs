using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Anthropic.SDK
{
    /// <summary>
    /// Interface for intercepting HTTP requests made by the Anthropic SDK.
    /// Allows users to implement custom retry logic, logging, metrics, or other cross-cutting concerns.
    /// </summary>
    public interface IRequestInterceptor
    {
        /// <summary>
        /// Intercepts an HTTP request and optionally wraps the call with custom logic.
        /// </summary>
        /// <param name="request">The HTTP request about to be sent</param>
        /// <param name="next">A delegate to invoke the next handler in the chain (or the actual HTTP call).
        /// Takes the request and cancellation token as parameters.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The HTTP response from the request</returns>
        /// <remarks>
        /// This method allows you to add retry logic, circuit breakers, logging, metrics, or other cross-cutting concerns.
        /// The interceptor is invoked for every HTTP request made by the SDK.
        /// You must call the next delegate to proceed with the actual HTTP request.
        /// The interceptor can modify the request before passing it to the next handler.
        /// </remarks>
        Task<HttpResponseMessage> InvokeAsync(
            HttpRequestMessage request,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next,
            CancellationToken cancellationToken = default);
    }
}
