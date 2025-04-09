using System.Reflection;

using Anthropic.SDK.Constants;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0001

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class SemanticKernelInitializationTests
    {
        [TestMethod]
        public async Task TestSKInit()
        {
            var skChatService =
                new ChatClientBuilder(new AnthropicClient().Messages)
                    .UseFunctionInvocation()
                    .Build()
                    .AsChatCompletionService();

            var sk = Kernel.CreateBuilder();
            sk.Plugins.AddFromType<SkPlugins>("Weather");
            sk.Services.AddSingleton<IChatCompletionService>(skChatService);

            var kernel = sk.Build();
            var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();
            // Create chat history
            var history = new ChatHistory();
            history.AddUserMessage("What is the weather like in San Francisco right now?");
            OpenAIPromptExecutionSettings promptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                ModelId = AnthropicModels.Claude35Haiku,
                MaxTokens = 512
            };

            // Get the response from the AI
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: promptExecutionSettings,
                kernel: kernel
            ); ;

            Assert.IsTrue(result.Content.Contains("72"));
        }

        [TestMethod]
        public async Task TestSKPDF()
        {
            var resourceName = "Anthropic.SDK.Tests.Claude3ModelCard.pdf";

            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            //read stream into byte array
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var pdfBytes = ms.ToArray();
            var base64String = Convert.ToBase64String(pdfBytes);

            var file = new File()
            {
                Name = "Claude3ModelCard.pdf",
                DataUri = "data:application/pdf;base64," + base64String
            };

            var client = new AnthropicClient();
            var skChatService = new ChatClientBuilder(client.Messages)
                .ConfigureOptions(opt =>
                {
                    opt.ModelId = AnthropicModels.Claude37Sonnet;
                    opt.MaxOutputTokens = 1024;
                })
                .UseFunctionInvocation()
                .Build()
                .AsChatCompletionService();

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddKeyedSingleton("test", skChatService);

            var kernel = kernelBuilder.Build();

            // Add plugins from the `SkPlugins` folder
            var pluginDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SummarizePlugin");
            kernel.ImportPluginFromPromptDirectory(pluginDirectoryPath);

            var filesSummary = await kernel.InvokeAsync<string>(
                "SummarizePlugin",
                "SummarizeDocuments",
                new() { { "fileName", file.Name }, { "fileDataUri", file.DataUri } }
            );
        }
    }

    public class File
    {
        public string Name { get; set; }
        public string DataUri { get; set; }
    }

    public class SkPlugins
    {
        [KernelFunction("GetWeather")]
        [Description("Gets the weather for a given location")]
        public async Task<string> GetWeather(string location)
        {
            return "It is 72 degrees and sunny in " + location;
        }
    }
}