using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Represents per-model pricing data for estimating API request costs.
    /// All costs are in USD per million tokens unless otherwise noted.
    /// </summary>
    public class ModelPricing
    {
        /// <summary>
        /// Cost per million input tokens.
        /// </summary>
        public decimal InputTokenCostPerMillion { get; }

        /// <summary>
        /// Cost per million output tokens.
        /// </summary>
        public decimal OutputTokenCostPerMillion { get; }

        /// <summary>
        /// Cost per million cache read tokens (typically 0.1x input cost).
        /// </summary>
        public decimal CacheReadCostPerMillion { get; }

        /// <summary>
        /// Cost per million 5-minute cache write tokens (typically 1.25x input cost).
        /// </summary>
        public decimal Cache5mWriteCostPerMillion { get; }

        /// <summary>
        /// Cost per million 1-hour cache write tokens (typically 2x input cost).
        /// </summary>
        public decimal Cache1hWriteCostPerMillion { get; }

        /// <summary>
        /// Cost per 1,000 web search requests. Default is $10 per 1,000 searches.
        /// </summary>
        public decimal WebSearchCostPer1000 { get; }

        public ModelPricing(
            decimal inputTokenCostPerMillion,
            decimal outputTokenCostPerMillion,
            decimal? cacheReadCostPerMillion = null,
            decimal? cache5mWriteCostPerMillion = null,
            decimal? cache1hWriteCostPerMillion = null,
            decimal? webSearchCostPer1000 = null)
        {
            InputTokenCostPerMillion = inputTokenCostPerMillion;
            OutputTokenCostPerMillion = outputTokenCostPerMillion;
            CacheReadCostPerMillion = cacheReadCostPerMillion ?? inputTokenCostPerMillion * 0.1m;
            Cache5mWriteCostPerMillion = cache5mWriteCostPerMillion ?? inputTokenCostPerMillion * 1.25m;
            Cache1hWriteCostPerMillion = cache1hWriteCostPerMillion ?? inputTokenCostPerMillion * 2m;
            WebSearchCostPer1000 = webSearchCostPer1000 ?? 10m;
        }

        private static readonly ConcurrentDictionary<string, ModelPricing> CustomPricing = new();

        // Ordered longest-prefix-first so that more specific entries match before shorter ones.
        private static readonly List<(string Prefix, ModelPricing Pricing)> BuiltInPricing = new()
        {
            // Opus 4.6 / 4.5 — $5 input, $25 output
            ("claude-opus-4-6", new ModelPricing(5m, 25m)),
            ("claude-opus-4-5", new ModelPricing(5m, 25m)),

            // Opus 4.1 — $15 input, $75 output
            ("claude-opus-4-1", new ModelPricing(15m, 75m)),

            // Opus 4 — $15 input, $75 output
            ("claude-opus-4", new ModelPricing(15m, 75m)),

            // Sonnet 4.6 — $3 input, $15 output
            ("claude-sonnet-4-6", new ModelPricing(3m, 15m)),

            // Sonnet 4.5 — $3 input, $15 output
            ("claude-sonnet-4-5", new ModelPricing(3m, 15m)),

            // Sonnet 4 — $3 input, $15 output
            ("claude-sonnet-4", new ModelPricing(3m, 15m)),

            // Sonnet 3.7 — $3 input, $15 output
            ("claude-3-7-sonnet", new ModelPricing(3m, 15m)),

            // Haiku 4.5 — $1 input, $5 output
            ("claude-haiku-4-5", new ModelPricing(1m, 5m)),

            // Haiku 3.5 — $0.80 input, $4 output
            ("claude-3-5-haiku", new ModelPricing(0.80m, 4m)),
        };

        /// <summary>
        /// Register or override pricing for a model ID prefix.
        /// Custom registrations take priority over built-in pricing.
        /// </summary>
        /// <param name="modelIdPrefix">The model ID or prefix to match (e.g. "claude-sonnet-4-6").</param>
        /// <param name="pricing">The pricing to use for matching models.</param>
        public static void Register(string modelIdPrefix, ModelPricing pricing)
        {
            if (string.IsNullOrWhiteSpace(modelIdPrefix))
                throw new ArgumentException("Model ID prefix cannot be null or empty.", nameof(modelIdPrefix));
            if (pricing == null)
                throw new ArgumentNullException(nameof(pricing));

            CustomPricing[modelIdPrefix] = pricing;
        }

        /// <summary>
        /// Remove a previously registered custom pricing entry.
        /// </summary>
        public static bool Unregister(string modelIdPrefix)
        {
            return CustomPricing.TryRemove(modelIdPrefix, out _);
        }

        /// <summary>
        /// Clear all custom pricing registrations, reverting to built-in pricing only.
        /// </summary>
        public static void ClearCustomPricing()
        {
            CustomPricing.Clear();
        }

        /// <summary>
        /// Look up pricing for a given model ID. Custom registrations are checked first
        /// (longest prefix match), then built-in pricing.
        /// </summary>
        /// <returns>The matching <see cref="ModelPricing"/>, or null if no match is found.</returns>
        public static ModelPricing ForModel(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return null;

            // Check custom registrations first (longest prefix match)
            if (!CustomPricing.IsEmpty)
            {
                var customMatch = CustomPricing.Keys
                    .Where(prefix => modelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(prefix => prefix.Length)
                    .FirstOrDefault();

                if (customMatch != null)
                    return CustomPricing[customMatch];
            }

            // Fall back to built-in pricing (already ordered longest-first)
            foreach (var (prefix, pricing) in BuiltInPricing)
            {
                if (modelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return pricing;
            }

            return null;
        }
    }
}
