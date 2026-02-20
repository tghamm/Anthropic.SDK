using System;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    /// <summary>
    /// Detailed breakdown of estimated costs for an API request.
    /// All values are in USD.
    /// </summary>
    public class CostBreakdown
    {
        /// <summary>
        /// Cost of base input tokens.
        /// </summary>
        public decimal InputTokenCost { get; set; }

        /// <summary>
        /// Cost of output tokens.
        /// </summary>
        public decimal OutputTokenCost { get; set; }

        /// <summary>
        /// Cost of cache read tokens.
        /// </summary>
        public decimal CacheReadCost { get; set; }

        /// <summary>
        /// Cost of cache creation tokens (combined 5-minute and 1-hour).
        /// When detailed cache creation breakdown is unavailable, the legacy
        /// <c>cache_creation_input_tokens</c> field is priced at the 5-minute write rate.
        /// </summary>
        public decimal CacheCreationCost { get; set; }

        /// <summary>
        /// Cost of web search requests ($0.01 per search).
        /// </summary>
        public decimal WebSearchCost { get; set; }

        /// <summary>
        /// Total estimated cost in USD (sum of all components).
        /// </summary>
        public decimal TotalCostUsd =>
            InputTokenCost + OutputTokenCost + CacheReadCost + CacheCreationCost + WebSearchCost;

        /// <summary>
        /// The <see cref="ModelPricing"/> used for this calculation.
        /// </summary>
        public ModelPricing Pricing { get; set; }
    }

    /// <summary>
    /// Extension methods for calculating estimated API costs from usage data.
    /// </summary>
    public static class CostCalculationExtensions
    {
        private const decimal PerMillionDivisor = 1_000_000m;
        private const decimal Per1000Divisor = 1_000m;

        /// <summary>
        /// Calculate the estimated cost of an API request from its <see cref="Usage"/> data.
        /// When the service tier is <see cref="ServiceTier.Batch"/>, a 50% discount is applied
        /// to all token costs automatically.
        /// </summary>
        /// <param name="usage">The usage data from the API response.</param>
        /// <param name="modelId">The model ID string used for the request.</param>
        /// <param name="overridePricing">
        /// Optional pricing to use instead of the built-in/registered pricing.
        /// </param>
        /// <returns>A <see cref="CostBreakdown"/> with per-category costs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="usage"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no pricing can be found for <paramref name="modelId"/>
        /// and <paramref name="overridePricing"/> is not provided.
        /// </exception>
        public static CostBreakdown CalculateCost(
            this Usage usage,
            string modelId,
            ModelPricing overridePricing = null)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            var pricing = overridePricing ?? ModelPricing.ForModel(modelId);
            if (pricing == null)
            {
                throw new InvalidOperationException(
                    $"No pricing found for model '{modelId}'. " +
                    "Use ModelPricing.Register() to add pricing, or pass overridePricing.");
            }

            decimal batchMultiplier = usage.ServiceTier == ServiceTier.Batch ? 0.5m : 1m;

            decimal inputCost = usage.InputTokens / PerMillionDivisor
                                * pricing.InputTokenCostPerMillion * batchMultiplier;

            decimal outputCost = usage.OutputTokens / PerMillionDivisor
                                 * pricing.OutputTokenCostPerMillion * batchMultiplier;

            decimal cacheReadCost = usage.CacheReadInputTokens / PerMillionDivisor
                                    * pricing.CacheReadCostPerMillion * batchMultiplier;

            decimal cacheCreationCost = 0m;

            if (usage.CacheCreation != null)
            {
                int tokens5m = usage.CacheCreation.Ephemeral5mInputTokens ?? 0;
                int tokens1h = usage.CacheCreation.Ephemeral1hInputTokens ?? 0;

                cacheCreationCost =
                    (tokens5m / PerMillionDivisor * pricing.Cache5mWriteCostPerMillion * batchMultiplier) +
                    (tokens1h / PerMillionDivisor * pricing.Cache1hWriteCostPerMillion * batchMultiplier);
            }

            if (usage.CacheCreationInputTokens > 0 && cacheCreationCost == 0m)
            {
                cacheCreationCost = usage.CacheCreationInputTokens / PerMillionDivisor
                                    * pricing.Cache5mWriteCostPerMillion * batchMultiplier;
            }

            decimal webSearchCost = 0m;
            if (usage.ServerToolUse?.WebSearchRequests is > 0)
            {
                webSearchCost = usage.ServerToolUse.WebSearchRequests.Value / Per1000Divisor
                                * pricing.WebSearchCostPer1000;
            }

            return new CostBreakdown
            {
                InputTokenCost = inputCost,
                OutputTokenCost = outputCost,
                CacheReadCost = cacheReadCost,
                CacheCreationCost = cacheCreationCost,
                WebSearchCost = webSearchCost,
                Pricing = pricing,
            };
        }

        /// <summary>
        /// Calculate the estimated cost of an API request directly from the <see cref="MessageResponse"/>.
        /// Uses the model from the response and its usage data.
        /// </summary>
        /// <param name="response">The message response from the API.</param>
        /// <param name="overridePricing">
        /// Optional pricing to use instead of the built-in/registered pricing.
        /// </param>
        /// <returns>A <see cref="CostBreakdown"/> with per-category costs.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="response"/> or its Usage is null.
        /// </exception>
        public static CostBreakdown CalculateCost(
            this MessageResponse response,
            ModelPricing overridePricing = null)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (response.Usage == null)
                throw new ArgumentNullException(nameof(response), "Response.Usage is null.");

            return response.Usage.CalculateCost(response.Model, overridePricing);
        }
    }
}
