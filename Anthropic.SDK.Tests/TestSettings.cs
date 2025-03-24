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

            // If settings aren't found, provide fallback values for local development
            if (string.IsNullOrEmpty(settings.VertexAIProjectId))
            {
                settings.VertexAIProjectId = "crv-engineering-ai-prd-8058"; // Default for local tests
            }

            if (string.IsNullOrEmpty(settings.VertexAIRegion))
            {
                settings.VertexAIRegion = "europe-west1"; // Default for local tests
            }

            return settings;
        }
    }
}