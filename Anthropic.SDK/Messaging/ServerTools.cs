using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    public class ServerTools
    {
        public static Common.Tool GetWebSearchTool(int maxUses = 5, List<string> allowedDomains = null,
            List<string> blockedDomains = null, UserLocation userLocation = null)
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
            
            return new Function("web_search", "web_search_20250305", dict);
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
