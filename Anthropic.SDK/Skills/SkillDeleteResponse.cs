using System.Text.Json.Serialization;

namespace Anthropic.SDK.Skills
{
    /// <summary>
    /// Response confirming skill deletion.
    /// </summary>
    public class SkillDeleteResponse
    {
        /// <summary>
        /// The ID of the deleted skill.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Deleted object type. For Skills, this is always "skill_deleted".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "skill_deleted";
    }
}
