using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class InputSchema
    {
        [JsonPropertyName("type")]
        public string type { get; set; } = "object";
        [JsonPropertyName("properties")]
        public Dictionary<string, Property> properties { get; set; }
        [JsonPropertyName("required")]
        public IList<string> required { get; set; }
    }

    public class Tool
    {
        [JsonPropertyName("name")]
        public string name { get; set; }
        [JsonPropertyName("description")]
        public string description { get; set; }
        [JsonPropertyName("input_schema")]
        public InputSchema input_schema { get; set; }
    }

    public class Property
    {
        [JsonPropertyName("type")]
        public string type { get; set; }
        [JsonPropertyName("enum")]
        public string[] @enum { get; set; }
        [JsonPropertyName("description")]
        public string description { get; set; }
    }
}
