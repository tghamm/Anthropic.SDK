using System;

namespace Anthropic.SDK.Common
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FunctionParameterAttribute : Attribute
    {
        /// <summary>
        /// Function parameter attribute to help describe the parameter for the function.
        /// </summary>
        /// <param name="description">
        /// The description of the parameter and its usage.
        /// </param>
        public FunctionParameterAttribute(string description, bool required)
        {
            Description = description;
            Required = required;
        }

        public string Description { get; }
        public bool Required { get; set; }
    }
}