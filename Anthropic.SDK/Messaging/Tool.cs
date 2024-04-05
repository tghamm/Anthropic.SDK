using System;
using System.Collections.Generic;
using System.Text;

namespace Anthropic.SDK.Messaging
{
    public class InputSchema
    {
        public string type { get; set; }
        public Dictionary<string, Property> properties { get; set; }
        public List<string> required { get; set; }
    }
    
    public class Tool
    {
        public string name { get; set; }
        public string description { get; set; }
        public InputSchema input_schema { get; set; }
    }

    public class Property
    {
        public string type { get; set; }
        public string description { get; set; }
    }

}
