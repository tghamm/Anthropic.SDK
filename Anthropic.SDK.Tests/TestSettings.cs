using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace Anthropic.SDK.Tests
{
    /// <summary>
    /// Configuration settings for tests
    /// </summary>
    public class TestSettings
    {
        /// <summary>
        /// VertexAI project ID for integration tests
        /// </summary>
        public string VertexAIProjectId { get; set; } = string.Empty;

        /// <summary>
        /// VertexAI region for integration tests
        /// </summary>
        public string VertexAIRegion { get; set; } = string.Empty;

        public string VertexAIAccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Loads test settings from appsettings.json
        /// </summary>
        /// <returns>The test settings</returns>
        public static TestSettings LoadSettings()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var settings = new TestSettings();
            configuration.GetSection("TestSettings").Bind(settings);

            if (string.IsNullOrEmpty(settings.VertexAIAccessToken))
            {
                var credential = GoogleCredential.GetApplicationDefault()
                    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

                settings.VertexAIAccessToken = credential.UnderlyingCredential.GetAccessTokenForRequestAsync().Result;
            }

            return settings;
        }
    }
}