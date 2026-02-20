using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    public class ServerTools
    {
        /// <summary>
        /// Default web search tool version with dynamic filtering support (requires code execution tool).
        /// </summary>
        public const string WebSearchVersionDynamicFiltering = "web_search_20260209";

        /// <summary>
        /// Legacy web search tool version without dynamic filtering.
        /// </summary>
        public const string WebSearchVersionLegacy = "web_search_20250305";

        /// <summary>
        /// Default web fetch tool version with dynamic filtering support (requires code execution tool).
        /// </summary>
        public const string WebFetchVersionDynamicFiltering = "web_fetch_20260209";

        /// <summary>
        /// Legacy web fetch tool version without dynamic filtering.
        /// </summary>
        public const string WebFetchVersionLegacy = "web_fetch_20250910";

        /// <summary>
        /// Creates a web search tool configuration.
        /// The default version (<c>web_search_20260209</c>) supports dynamic filtering with Claude Opus 4.6 and Sonnet 4.6,
        /// which requires the code execution tool to be enabled. Pass <see cref="WebSearchVersionLegacy"/> for the older version.
        /// </summary>
        public static Common.Tool GetWebSearchTool(int maxUses = 5, List<string> allowedDomains = null,
            List<string> blockedDomains = null, UserLocation userLocation = null,
            string toolVersion = null)
        {
            var dict = new Dictionary<string, object>();
            dict.Add("max_uses", maxUses);

            if (allowedDomains != null && allowedDomains.Count > 0)
            {
                dict.Add("allowed_domains", allowedDomains);
            }
            if (blockedDomains != null && blockedDomains.Count > 0)
            {
                dict.Add("blocked_domains", blockedDomains);
            }
            if (userLocation != null)
            {
                dict.Add("user_location", userLocation);
            }

            return new Function("web_search", toolVersion ?? WebSearchVersionDynamicFiltering, dict);
        }

        /// <summary>
        /// Creates a web fetch tool configuration.
        /// The default version (<c>web_fetch_20260209</c>) supports dynamic filtering with Claude Opus 4.6 and Sonnet 4.6,
        /// which requires the code execution tool to be enabled. Pass <see cref="WebFetchVersionLegacy"/> for the older version.
        /// </summary>
        public static Common.Tool GetWebFetchTool(int? maxUses = null,
            List<string> allowedDomains = null,
            List<string> blockedDomains = null,
            bool enableCitations = false,
            int? maxContentTokens = null,
            string toolVersion = null)
        {
            var dict = new Dictionary<string, object>();

            if (maxUses.HasValue)
            {
                dict.Add("max_uses", maxUses.Value);
            }
            if (allowedDomains != null && allowedDomains.Count > 0)
            {
                dict.Add("allowed_domains", allowedDomains);
            }
            if (blockedDomains != null && blockedDomains.Count > 0)
            {
                dict.Add("blocked_domains", blockedDomains);
            }
            if (enableCitations)
            {
                dict.Add("citations", new Dictionary<string, object> { { "enabled", true } });
            }
            if (maxContentTokens.HasValue)
            {
                dict.Add("max_content_tokens", maxContentTokens.Value);
            }

            return new Function("web_fetch", toolVersion ?? WebFetchVersionDynamicFiltering, dict);
        }

        public static Common.Tool GetCodeExecutionTool()
        {
            var dict = new Dictionary<string, object>();
            return new Function("code_execution", "code_execution_20250825", dict);
        }
    }


    public class UserLocation
    {
        [JsonPropertyName("type")]
        public string Type => "approximate";
        [JsonPropertyName("city")]
        public string City { get; set; }
        [JsonPropertyName("region")]
        public string Region { get; set; }
        [JsonPropertyName("country")]
        public string Country { get; set; }
        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }
    }
}
