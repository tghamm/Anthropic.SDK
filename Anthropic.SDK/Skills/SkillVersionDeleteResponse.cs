using System.Text.Json.Serialization;

namespace Anthropic.SDK.Skills
{
    /// <summary>
    /// Response confirming skill version deletion.
    /// </summary>
    public class SkillVersionDeleteResponse
    {
        /// <summary>
        /// Version identifier for the skill.
        /// Each version is identified by a Unix epoch timestamp (e.g., "1759178010641129").
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Deleted object type. For Skill Versions, this is always "skill_version_deleted".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "skill_version_deleted";
    }
}
