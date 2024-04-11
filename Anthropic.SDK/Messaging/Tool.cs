using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Tool Input Schema Class
    /// </summary>
    public class InputSchema
    {
        /// <summary>
        /// Type of the Schema, default is object
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";
        
        /// <summary>
        /// Properties of the Schema
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, Property> Properties { get; set; }
        
        /// <summary>
        /// Required Properties
        /// </summary>
        [JsonPropertyName("required")]
        public IList<string> Required { get; set; }
    }

    /// <summary>
    /// Serializable Tool Class
    /// </summary>
    public class Tool
    {
        /// <summary>
        /// Tool Name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Tool Description
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        /// <summary>
        /// Tool Input Schema
        /// </summary>
        [JsonPropertyName("input_schema")]
        public InputSchema InputSchema { get; set; }
    }

    /// <summary>
    /// Property Definition Class
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Property Type
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Enum Values as Strings (if applicable)
        /// </summary>
        [JsonPropertyName("enum")]
        public string[] Enum { get; set; }
        
        /// <summary>
        /// Description of the Property
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
