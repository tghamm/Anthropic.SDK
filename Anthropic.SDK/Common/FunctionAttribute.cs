using System;

namespace Anthropic.SDK.Common
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class FunctionAttribute : Attribute
    {
        public FunctionAttribute(string description = null)
        {
            Description = description;
        }

        public string Description { get; }
    }
}