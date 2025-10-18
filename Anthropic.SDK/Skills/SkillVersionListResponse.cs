using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Skills
{
    /// <summary>
    /// Response containing a paginated list of skill versions.
    /// </summary>
    public class SkillVersionListResponse
    {
        /// <summary>
        /// List of skill versions.
        /// </summary>
        [JsonPropertyName("data")]
        public List<SkillVersionResponse> Data { get; set; }

        /// <summary>
        /// Indicates if there are more results in the requested page direction.
        /// </summary>
        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        /// <summary>
        /// Token to provide as 'page' in the subsequent request to retrieve the next page of data.
        /// </summary>
        [JsonPropertyName("next_page")]
        public string NextPage { get; set; }
    }
}
