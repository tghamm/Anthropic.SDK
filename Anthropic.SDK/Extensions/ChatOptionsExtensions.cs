using System;
using Microsoft.Extensions.AI;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    /// <summary>
    /// Extensions for ChatOptions to support Anthropic-specific features
    /// </summary>
    public static class ChatOptionsExtensions
    {
        private const string ThinkingParametersKey = "Anthropic.ThinkingParameters";

        /// <summary>
        /// Sets thinking parameters for extended thinking support in compatible models like Claude 3.7 Sonnet
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <param name="budgetTokens">The budget tokens for thinking (typically 8000-32000)</param>
        /// <returns>The ChatOptions instance for fluent chaining</returns>
        public static ChatOptions WithThinking(this ChatOptions options, int budgetTokens)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (budgetTokens <= 0)
                throw new ArgumentOutOfRangeException(nameof(budgetTokens), "Budget tokens must be greater than 0");

            (options.AdditionalProperties ??= new())[ThinkingParametersKey] = new ThinkingParameters
            {
                BudgetTokens = budgetTokens
            };

            return options;
        }

        /// <summary>
        /// Sets thinking parameters for extended thinking support in compatible models like Claude 3.7 Sonnet
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <param name="thinkingParameters">The thinking parameters to set</param>
        /// <returns>The ChatOptions instance for fluent chaining</returns>
        public static ChatOptions WithThinking(this ChatOptions options, ThinkingParameters thinkingParameters)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (thinkingParameters == null)
                throw new ArgumentNullException(nameof(thinkingParameters));

            (options.AdditionalProperties ??= new())[ThinkingParametersKey] = thinkingParameters;

            return options;
        }

        /// <summary>
        /// Gets the thinking parameters from ChatOptions
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <returns>The thinking parameters, or null if not set</returns>
        public static ThinkingParameters GetThinkingParameters(this ChatOptions options)
        {
            if (options?.AdditionalProperties?.TryGetValue(ThinkingParametersKey, out var value) == true)
            {
                return value as ThinkingParameters;
            }

            return null;
        }
    }
}