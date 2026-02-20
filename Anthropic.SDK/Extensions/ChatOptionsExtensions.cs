using System;
using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    /// <summary>
    /// Configuration for the web fetch tool when used via IChatClient.
    /// </summary>
    public class WebFetchConfiguration
    {
        public int? MaxUses { get; set; }
        public List<string> AllowedDomains { get; set; }
        public List<string> BlockedDomains { get; set; }
        public bool EnableCitations { get; set; }
        public int? MaxContentTokens { get; set; }
    }

    /// <summary>
    /// Extensions for ChatOptions to support Anthropic-specific features
    /// </summary>
    public static class ChatOptionsExtensions
    {
        private const string ThinkingParametersKey = "Anthropic.ThinkingParameters";
        private const string StrictToolsKey = "Anthropic.StrictTools";
        private const string WebFetchKey = "Anthropic.WebFetch";

        /// <summary>
        /// Sets thinking parameters for extended thinking support in compatible models like Claude 3.7 Sonnet
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <param name="budgetTokens">The budget tokens for thinking (typically up to max_tokens unless using interleaved thinking)</param>
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
        /// Sets interleaved thinking parameters for enhanced thinking support. This enables the interleaved-thinking-2025-05-14 beta header
        /// which allows thinking tokens to exceed max_tokens. Note: On 3rd-party platforms (Bedrock, Vertex AI), this only works 
        /// with Claude Opus 4.1, Opus 4, or Sonnet 4 models.
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <param name="budgetTokens">The budget tokens for thinking (can exceed max_tokens when using interleaved thinking)</param>
        /// <returns>The ChatOptions instance for fluent chaining</returns>
        public static ChatOptions WithInterleavedThinking(this ChatOptions options, int budgetTokens)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (budgetTokens <= 0)
                throw new ArgumentOutOfRangeException(nameof(budgetTokens), "Budget tokens must be greater than 0");

            (options.AdditionalProperties ??= new())[ThinkingParametersKey] = new ThinkingParameters
            {
                BudgetTokens = budgetTokens,
                UseInterleavedThinking = true
            };

            return options;
        }

        /// <summary>
        /// Sets interleaved thinking parameters for enhanced thinking support. This enables the interleaved-thinking-2025-05-14 beta header
        /// which allows thinking tokens to exceed max_tokens. Note: On 3rd-party platforms (Bedrock, Vertex AI), this only works 
        /// with Claude Opus 4.1, Opus 4, or Sonnet 4 models.
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <param name="thinkingParameters">The thinking parameters to set</param>
        /// <returns>The ChatOptions instance for fluent chaining</returns>
        public static ChatOptions WithInterleavedThinking(this ChatOptions options, ThinkingParameters thinkingParameters)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (thinkingParameters == null)
                throw new ArgumentNullException(nameof(thinkingParameters));

            thinkingParameters.UseInterleavedThinking = true;
            (options.AdditionalProperties ??= new())[ThinkingParametersKey] = thinkingParameters;

            return options;
        }

        /// <summary>
        /// Sets adaptive thinking mode, which lets Claude dynamically determine when and how much to use extended thinking.
        /// Recommended for Claude Opus 4.6 and Sonnet 4.6. Interleaved thinking is automatically enabled.
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <returns>The ChatOptions instance for fluent chaining</returns>
        public static ChatOptions WithAdaptiveThinking(this ChatOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            (options.AdditionalProperties ??= new())[ThinkingParametersKey] = new ThinkingParameters
            {
                Type = ThinkingType.adaptive
            };

            return options;
        }

        /// <summary>
        /// Sets adaptive thinking mode with a specific effort level. The effort level is mapped to output_config.effort
        /// to guide how much thinking Claude does. Recommended for Claude Opus 4.6 and Sonnet 4.6.
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <param name="effort">The effort level (low, medium, high, or max) to guide thinking allocation</param>
        /// <returns>The ChatOptions instance for fluent chaining</returns>
        public static ChatOptions WithAdaptiveThinking(this ChatOptions options, ThinkingEffort effort)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            (options.AdditionalProperties ??= new())[ThinkingParametersKey] = new ThinkingParameters
            {
                Type = ThinkingType.adaptive,
                Effort = effort
            };

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

        /// <summary>
        /// Enables strict mode for tool use, which guarantees that tool inputs conform exactly to the schema.
        /// Requires the structured-outputs-2025-11-13 beta header (automatically added).
        /// Note: Strict tools are automatically enabled when using ResponseFormat with a JSON schema.
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <param name="strict">Whether to enable strict mode (default: true)</param>
        /// <returns>The ChatOptions instance for fluent chaining</returns>
        public static ChatOptions WithStrictTools(this ChatOptions options, bool strict = true)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            (options.AdditionalProperties ??= new())[StrictToolsKey] = strict;

            return options;
        }

        /// <summary>
        /// Gets whether strict tools mode is enabled from ChatOptions
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <returns>True if strict tools mode is enabled, false otherwise</returns>
        public static bool GetStrictToolsEnabled(this ChatOptions options)
        {
            if (options?.AdditionalProperties?.TryGetValue(StrictToolsKey, out var value) == true)
            {
                return value is bool b && b;
            }

            return false;
        }

        /// <summary>
        /// Enables the web fetch tool for IChatClient requests. Since Microsoft.Extensions.AI does not
        /// have a built-in HostedWebFetchTool, this extension method provides an Anthropic-specific way
        /// to enable web fetch capability.
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <param name="maxUses">Optional limit on the number of fetches per request</param>
        /// <param name="allowedDomains">Optional list of domains to allow fetching from</param>
        /// <param name="blockedDomains">Optional list of domains to block fetching from</param>
        /// <param name="enableCitations">Whether to enable citations for fetched content</param>
        /// <param name="maxContentTokens">Optional maximum content length in tokens</param>
        /// <returns>The ChatOptions instance for fluent chaining</returns>
        public static ChatOptions WithWebFetch(this ChatOptions options,
            int? maxUses = null,
            List<string> allowedDomains = null,
            List<string> blockedDomains = null,
            bool enableCitations = false,
            int? maxContentTokens = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            (options.AdditionalProperties ??= new())[WebFetchKey] = new WebFetchConfiguration
            {
                MaxUses = maxUses,
                AllowedDomains = allowedDomains,
                BlockedDomains = blockedDomains,
                EnableCitations = enableCitations,
                MaxContentTokens = maxContentTokens
            };

            return options;
        }

        /// <summary>
        /// Gets the web fetch configuration from ChatOptions, if set.
        /// </summary>
        /// <param name="options">The ChatOptions instance</param>
        /// <returns>The web fetch configuration, or null if not set</returns>
        public static WebFetchConfiguration GetWebFetchConfiguration(this ChatOptions options)
        {
            if (options?.AdditionalProperties?.TryGetValue(WebFetchKey, out var value) == true)
            {
                return value as WebFetchConfiguration;
            }

            return null;
        }
    }
}