using System.Text.Json.Serialization;

namespace Anthropic.SDK.Skills
{
    /// <summary>
    /// Response from creating or retrieving a skill.
    /// </summary>
    public class SkillResponse
    {
        /// <summary>
        /// Unique identifier for the skill.
        /// The format and length of IDs may change over time.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Object type. For Skills, this is always "skill".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "skill";

        /// <summary>
        /// Display title for the skill.
        /// This is a human-readable label that is not included in the prompt sent to the model.
        /// </summary>
        [JsonPropertyName("display_title")]
        public string DisplayTitle { get; set; }

        /// <summary>
        /// Source of the skill.
        /// This may be one of the following values:
        /// * "custom": the skill was created by a user
        /// * "anthropic": the skill was created by Anthropic
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; }

        /// <summary>
        /// The latest version identifier for the skill.
        /// This represents the most recent version of the skill that has been created.
        /// </summary>
        [JsonPropertyName("latest_version")]
        public string LatestVersion { get; set; }

        /// <summary>
        /// ISO 8601 timestamp of when the skill was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        /// <summary>
        /// ISO 8601 timestamp of when the skill was last updated.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }
    }
}
