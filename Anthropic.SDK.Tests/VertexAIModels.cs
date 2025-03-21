using Anthropic.SDK.Constants;
using Anthropic.SDK.Models;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class VertexAIModels
    {
        // Mock credentials for testing - these won't actually be used in tests
        private const string TestProjectId = "test-project-id";
        private const string TestRegion = "us-central1";

        [TestMethod]
        public async Task TestListModels()
        {
            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion));
            
            try
            {
                var models = await client.Models.ListModelsAsync();
                
                // Verify that the models list contains the expected models
                Assert.IsNotNull(models);
                Assert.IsNotNull(models.Models);
                Assert.IsTrue(models.Models.Count > 0);
                
                // Check for specific models
                Assert.IsTrue(models.Models.Any(m => m.Id == Constants.VertexAIModels.Claude3Opus));
                Assert.IsTrue(models.Models.Any(m => m.Id == Constants.VertexAIModels.Claude3Sonnet));
                Assert.IsTrue(models.Models.Any(m => m.Id == Constants.VertexAIModels.Claude3Haiku));
                Assert.IsTrue(models.Models.Any(m => m.Id == Constants.VertexAIModels.Claude35Sonnet));
                Assert.IsTrue(models.Models.Any(m => m.Id == Constants.VertexAIModels.Claude35Haiku));
                Assert.IsTrue(models.Models.Any(m => m.Id == Constants.VertexAIModels.Claude37Sonnet));
            }
            catch (Exception ex)
            {
                // Since this is a local implementation that doesn't actually call the API,
                // we don't expect authentication errors here
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task TestRetrieveModel()
        {
            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion));
            
            try
            {
                // Test retrieving Claude 3 Opus model
                var opusModel = await client.Models.RetrieveModelAsync(Constants.VertexAIModels.Claude3Opus);
                Assert.IsNotNull(opusModel);
                Assert.AreEqual(Constants.VertexAIModels.Claude3Opus, opusModel.Id);
                Assert.AreEqual("Claude 3 Opus (Vertex AI)", opusModel.DisplayName);
                Assert.AreEqual("model", opusModel.Type);
                
                // Test retrieving Claude 3 Sonnet model
                var sonnetModel = await client.Models.RetrieveModelAsync(Constants.VertexAIModels.Claude3Sonnet);
                Assert.IsNotNull(sonnetModel);
                Assert.AreEqual(Constants.VertexAIModels.Claude3Sonnet, sonnetModel.Id);
                Assert.AreEqual("Claude 3 Sonnet (Vertex AI)", sonnetModel.DisplayName);
                Assert.AreEqual("model", sonnetModel.Type);
                
                // Test retrieving Claude 3 Haiku model
                var haikuModel = await client.Models.RetrieveModelAsync(Constants.VertexAIModels.Claude3Haiku);
                Assert.IsNotNull(haikuModel);
                Assert.AreEqual(Constants.VertexAIModels.Claude3Haiku, haikuModel.Id);
                Assert.AreEqual("Claude 3 Haiku (Vertex AI)", haikuModel.DisplayName);
                Assert.AreEqual("model", haikuModel.Type);
                
                // Test retrieving Claude 3.5 Sonnet model
                var sonnet35Model = await client.Models.RetrieveModelAsync(Constants.VertexAIModels.Claude35Sonnet);
                Assert.IsNotNull(sonnet35Model);
                Assert.AreEqual(Constants.VertexAIModels.Claude35Sonnet, sonnet35Model.Id);
                Assert.AreEqual("Claude 3.5 Sonnet (Vertex AI)", sonnet35Model.DisplayName);
                Assert.AreEqual("model", sonnet35Model.Type);
                
                // Test retrieving Claude 3.5 Haiku model
                var haiku35Model = await client.Models.RetrieveModelAsync(Constants.VertexAIModels.Claude35Haiku);
                Assert.IsNotNull(haiku35Model);
                Assert.AreEqual(Constants.VertexAIModels.Claude35Haiku, haiku35Model.Id);
                Assert.AreEqual("Claude 3.5 Haiku (Vertex AI)", haiku35Model.DisplayName);
                Assert.AreEqual("model", haiku35Model.Type);
                
                // Test retrieving Claude 3.7 Sonnet model
                var sonnet37Model = await client.Models.RetrieveModelAsync(Constants.VertexAIModels.Claude37Sonnet);
                Assert.IsNotNull(sonnet37Model);
                Assert.AreEqual(Constants.VertexAIModels.Claude37Sonnet, sonnet37Model.Id);
                Assert.AreEqual("Claude 3.7 Sonnet (Vertex AI)", sonnet37Model.DisplayName);
                Assert.AreEqual("model", sonnet37Model.Type);
                
                // Test retrieving a non-existent model
                var nonExistentModel = await client.Models.RetrieveModelAsync("non-existent-model");
                Assert.IsNull(nonExistentModel);
            }
            catch (Exception ex)
            {
                // Since this is a local implementation that doesn't actually call the API,
                // we don't expect authentication errors here
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }
    }
}