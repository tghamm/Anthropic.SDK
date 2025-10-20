using System.Text.Json.Serialization;

namespace Anthropic.SDK.Skills
{
    /// <summary>
    /// Response from creating or retrieving a skill version.
    /// </summary>
    public class SkillVersionResponse
    {
        /// <summary>
        /// Unique identifier for the skill version.
        /// The format and length of IDs may change over time.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Object type. For Skill Versions, this is always "skill_version".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "skill_version";

        /// <summary>
        /// Identifier for the skill that this version belongs to.
        /// </summary>
        [JsonPropertyName("skill_id")]
        public string SkillId { get; set; }

        /// <summary>
        /// Version identifier for the skill.
        /// Each version is identified by a Unix epoch timestamp (e.g., "1759178010641129").
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; }

        /// <summary>
        /// Human-readable name of the skill version.
        /// This is extracted from the SKILL.md file in the skill upload.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Description of the skill version.
        /// This is extracted from the SKILL.md file in the skill upload.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Directory name of the skill version.
        /// This is the top-level directory name that was extracted from the uploaded files.
        /// </summary>
        [JsonPropertyName("directory")]
        public string Directory { get; set; }

        /// <summary>
        /// ISO 8601 timestamp of when the skill version was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
    }
}
