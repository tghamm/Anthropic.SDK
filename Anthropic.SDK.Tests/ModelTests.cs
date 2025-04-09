namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class ModelTests
    {
        [TestMethod]
        public async Task TestModelEndpointFunctionality()
        {
            var client = new AnthropicClient();
            var res = await client.Models.ListModelsAsync();
            Assert.IsNotNull(res.Models);
            var modelId = res.Models.First().Id;
            var model = await client.Models.GetModelAsync(modelId);
            Assert.IsNotNull(model);
        }
    }
}