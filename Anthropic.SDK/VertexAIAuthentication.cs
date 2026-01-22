using System;
using System.Collections.Generic;
using System.Text;

namespace Anthropic.SDK
{
    /// <summary>
    /// Authentication for Google Cloud Vertex AI
    /// </summary>
    public class VertexAIAuthentication
    {
        /// <summary>
        /// The Google Cloud Project ID
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// The Google Cloud Region (e.g., "us-east5")
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// The Google Cloud API Key (optional, can use default credentials)
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The OAuth2 Access Token (optional, can use default credentials)
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Instantiates a new Vertex AI Authentication object with the given parameters
        /// </summary>
        /// <param name="projectId">The Google Cloud Project ID</param>
        /// <param name="region">The Google Cloud Region (e.g., "us-east5")</param>
        /// <param name="apiKey">The Google Cloud API Key (optional)</param>
        /// <param name="accessToken">The OAuth2 Access Token (optional)</param>
        public VertexAIAuthentication(string projectId, string region, string apiKey = null, string accessToken = null)
        {
            this.ProjectId = projectId;
            this.Region = region;
            this.ApiKey = apiKey;
            this.AccessToken = accessToken;
        }

        private static volatile VertexAIAuthentication _cachedDefault;
        private static readonly object _defaultLock = new object();

        /// <summary>
        /// The default authentication to use when no other auth is specified. This can be set manually, or automatically loaded via environment variables.
        /// </summary>
        public static VertexAIAuthentication Default
        {
            get
            {
                if (_cachedDefault != null)
                    return _cachedDefault;

                lock (_defaultLock)
                {
                    if (_cachedDefault != null)
                        return _cachedDefault;
                    
                    _cachedDefault = LoadFromEnv();
                    return _cachedDefault;
                }
            }
            set
            {
                lock (_defaultLock)
                {
                    _cachedDefault = value;
                }
            }
        }

        /// <summary>
        /// Attempts to load Vertex AI authentication from environment variables:
        /// - GOOGLE_CLOUD_PROJECT: The Google Cloud Project ID
        /// - GOOGLE_CLOUD_REGION: The Google Cloud Region
        /// - GOOGLE_API_KEY: The Google Cloud API Key (optional)
        /// - GOOGLE_ACCESS_TOKEN: The OAuth2 Access Token (optional)
        /// </summary>
        /// <returns>Returns the loaded <see cref="VertexAIAuthentication"/> if environment variables were found, or <see langword="null"/> if there were no matching environment vars.</returns>
        public static VertexAIAuthentication LoadFromEnv()
        {
            string projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
            string region = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_REGION");
            string apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
            string accessToken = Environment.GetEnvironmentVariable("GOOGLE_ACCESS_TOKEN");

            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(region))
                return null;

            return new VertexAIAuthentication(projectId, region, apiKey, accessToken);
        }
    }

    internal static class VertexAIAuthHelpers
    {
        /// <summary>
        /// A helper method to swap out <see langword="null"/> <see cref="VertexAIAuthentication"/> objects with the <see cref="VertexAIAuthentication.Default"/> authentication, possibly loaded from ENV.
        /// </summary>
        /// <param name="auth">The specific authentication to use if not <see langword="null"/></param>
        /// <returns>Either the provided <paramref name="auth"/> or the <see cref="VertexAIAuthentication.Default"/></returns>
        public static VertexAIAuthentication ThisOrDefault(this VertexAIAuthentication auth)
        {
            if (auth == null)
                auth = VertexAIAuthentication.Default;

            return auth;
        }
    }
}