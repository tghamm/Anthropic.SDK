using System;

namespace Anthropic.SDK.Constants
{


    /// <summary>
    /// Constants that represent Anthropic Models.
    /// </summary>
    public static class AnthropicModels
    {
        /// <summary>
        /// Claude 3 Opus
        /// </summary>
        [Obsolete ("This Model will be retired on January 5th 2026")]
        public const string Claude3Opus = "claude-3-opus-20240229";
        
        /// <summary>
        /// Claude 3.5 Sonnet
        /// </summary>
        public const string Claude35Sonnet = "claude-3-5-sonnet-20241022";
        
        /// <summary>
        /// Claude 3.7 Sonnet
        /// </summary>
        public const string Claude37Sonnet = "claude-3-7-sonnet-20250219";

        /// <summary>
        /// Claude 4 Sonnet
        /// </summary>
        public const string Claude4Sonnet = "claude-sonnet-4-20250514";

        /// <summary>
        /// Claude 4.5 Sonnet
        /// </summary>
        public const string Claude45Sonnet = "claude-sonnet-4-5-20250929";

        /// <summary>
        /// Claude 4 Opus
        /// </summary>
        public const string Claude4Opus = "claude-opus-4-20250514";

        /// <summary>
        /// Claude 4.1 Opus
        /// </summary>
        public const string Claude41Opus = "claude-opus-4-1-20250805";        

        /// <summary>
        /// Claude 3.5 Haiku
        /// </summary>
        public const string Claude35Haiku = "claude-3-5-haiku-20241022";

        /// <summary>
        /// Claude 3 Haiku
        /// </summary>
        public const string Claude3Haiku = "claude-3-haiku-20240307";
    }
}
