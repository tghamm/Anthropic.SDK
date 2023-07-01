using System;
using System.Collections.Generic;
using System.Text;

namespace Anthropic.SDK
{
    public class APIAuthentication
    {
        /// <summary>
        /// The API key, required to access the API endpoint.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Allows implicit casting from a string, so that a simple string API key can be provided in place of an instance of <see cref="APIAuthentication"/>
        /// </summary>
        /// <param name="key">The API key to convert into a <see cref="APIAuthentication"/>.</param>
        public static implicit operator APIAuthentication(string key)
        {
            return new APIAuthentication(key);
        }

        /// <summary>
        /// Instantiates a new Authentication object with the given <paramref name="apiKey"/>, which may be <see langword="null"/>.
        /// </summary>
        /// <param name="apiKey">The API key, required to access the API endpoint.</param>
        public APIAuthentication(string apiKey)
        {
            this.ApiKey = apiKey;
        }

        private static APIAuthentication _cachedDefault = null;

        /// <summary>
        /// The default authentication to use when no other auth is specified.  This can be set manually, or automatically loaded via environment variables.  <seealso cref="LoadFromEnv"/>
        /// </summary>
        public static APIAuthentication Default
        {
            get
            {
                if (_cachedDefault != null)
                    return _cachedDefault;

                APIAuthentication auth = LoadFromEnv();
                
                _cachedDefault = auth;
                return auth;
            }
            set
            {
                _cachedDefault = value;
            }
        }

        /// <summary>
        /// Attempts to load api key from environment variables, as "ANTHROPIC_API_KEY".
        /// </summary>
        /// <returns>Returns the loaded <see cref="APIAuthentication"/> any api keys were found, or <see langword="null"/> if there were no matching environment vars.</returns>
        public static APIAuthentication LoadFromEnv()
        {
            string key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

            if (string.IsNullOrEmpty(key))
                return null;

            return new APIAuthentication(key);
        }
    }

    internal static class AuthHelpers
    {
        /// <summary>
        /// A helper method to swap out <see langword="null"/> <see cref="APIAuthentication"/> objects with the <see cref="APIAuthentication.Default"/> authentication, possibly loaded from ENV.
        /// </summary>
        /// <param name="auth">The specific authentication to use if not <see langword="null"/></param>
        /// <returns>Either the provided <paramref name="auth"/> or the <see cref="APIAuthentication.Default"/></returns>
        public static APIAuthentication ThisOrDefault(this APIAuthentication auth)
        {
            if (auth == null)
                auth = APIAuthentication.Default;

            return auth;
        }
    }
}
