using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Skills
{
    /// <summary>
    /// Response containing a paginated list of skills.
    /// </summary>
    public class SkillListResponse
    {
        /// <summary>
        /// List of skill objects.
        /// </summary>
        [JsonPropertyName("data")]
        public List<SkillResponse> Data { get; set; }

        /// <summary>
        /// Whether there are more results available.
        /// If true, there are additional results that can be fetched using the next_page token.
        /// </summary>
        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        /// <summary>
        /// Token for fetching the next page of results.
        /// If null, there are no more results available. Pass this value to the page parameter in the next request to get the next page.
        /// </summary>
        [JsonPropertyName("next_page")]
        public string NextPage { get; set; }
    }
}
