using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Container configuration for Skills API
    /// </summary>
    public class Container
    {
        /// <summary>
        /// Optional container ID to reuse an existing container from a previous request
        /// </summary>
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; set; }

        /// <summary>
        /// List of skills to enable in the container (max 8 skills)
        /// </summary>
        [JsonPropertyName("skills")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Skill> Skills { get; set; }
    }

    /// <summary>
    /// Skill definition for use in containers
    /// </summary>
    public class Skill
    {
        /// <summary>
        /// Type of skill - either "anthropic" for built-in skills or "custom" for custom skills
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// Unique identifier for the skill
        /// For anthropic type: "pptx", "xlsx", "docx", "pdf"
        /// For custom type: a custom string identifier
        /// </summary>
        [JsonPropertyName("skill_id")]
        public string SkillId { get; set; }

        /// <summary>
        /// Optional version to pin to a specific version (e.g., "latest")
        /// </summary>
        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Version { get; set; }
    }

    /// <summary>
    /// Container information returned in the response
    /// </summary>
    public class ContainerResponse
    {
        /// <summary>
        /// The container ID that can be reused in subsequent requests
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The expiration date for the container
        /// </summary>
        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }
    }
}
