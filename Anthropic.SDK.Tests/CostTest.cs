using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Extensions;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class CostTest
    {
       
        [TestMethod]
        public async Task TestCostEstimation()
        {
            var client = new AnthropicClient();
            var parameters = new MessageParameters()
            {
                Messages = new List<Message> { new Message(RoleType.User, "Hello!") },
                MaxTokens = 1024,
                Model = AnthropicModels.Claude46Sonnet,
            };
            var response = await client.Messages.GetClaudeMessageAsync(parameters);

            // Get total estimated cost
            var cost = response.CalculateCost();
            Console.WriteLine($"Total cost: ${cost.TotalCostUsd:F6}");
            Console.WriteLine($"  Input tokens: ${cost.InputTokenCost:F6}");
            Console.WriteLine($"  Output tokens: ${cost.OutputTokenCost:F6}");
            Console.WriteLine($"  Cache read: ${cost.CacheReadCost:F6}");
            Console.WriteLine($"  Cache creation: ${cost.CacheCreationCost:F6}");
            Console.WriteLine($"  Web search: ${cost.WebSearchCost:F6}");
        }
    }
}
