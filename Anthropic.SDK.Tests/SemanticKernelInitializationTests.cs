using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using static Google.Rpc.Context.AttributeContext.Types;
#pragma warning disable SKEXP0001

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class SemanticKernelInitializationTests
    {
        [TestMethod]
        public async Task TestSKInit()
        {
            IChatClient CreateChatClient(IServiceProvider _)
                => new ChatClientBuilder(new AnthropicClient().Messages)
                    .UseFunctionInvocation()
                    .Build();

            var sk = Kernel.CreateBuilder();
            sk.Plugins.AddFromType<SkPlugins>("Weather");
            sk.Services.AddSingleton(CreateChatClient);

            var kernel = sk.Build();
            var chatClient = kernel.Services.GetRequiredService<IChatClient>();

            // Create chat history
            List<ChatMessage> messages = [new(ChatRole.User, "What is the weather like in San Francisco right now?")];

            var skExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                ModelId = AnthropicModels.Claude45Haiku,
                MaxTokens = 512
            };

            // Get the response from the AI
            var result = await chatClient.GetResponseAsync(messages, options: skExecutionSettings.ToChatOptions(kernel));

            Assert.IsTrue(result.Text.Contains("72"));
        }

        [TestMethod]
        public async Task TestSKPDF()
        {
            string resourceName = "Anthropic.SDK.Tests.Claude3ModelCard.pdf";

            Assembly assembly = Assembly.GetExecutingAssembly();

            await using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            //read stream into byte array
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            byte[] pdfBytes = ms.ToArray();
            string base64String = Convert.ToBase64String(pdfBytes);

            var file = new File()
            {
                Name = "Claude3ModelCard.pdf",
                DataUri = "data:application/pdf;base64," + base64String
            };

            AnthropicClient client = new AnthropicClient();
            IChatCompletionService skChatService = new ChatClientBuilder(client.Messages)
                .ConfigureOptions(opt =>
                {
                    opt.ModelId = AnthropicModels.Claude46Sonnet;
                    opt.MaxOutputTokens = 1024;
                })
                .UseFunctionInvocation()
                .Build()
                .AsChatCompletionService();

            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddKeyedSingleton("test", skChatService);

            Kernel kernel = kernelBuilder.Build();

            // Add plugins from the `SkPlugins` folder
            string pluginDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SummarizePlugin");
            kernel.ImportPluginFromPromptDirectory(pluginDirectoryPath);

            string? filesSummary = await kernel.InvokeAsync<string>(
                "SummarizePlugin",
                "SummarizeDocuments",
                new() { { "fileName", file.Name }, { "fileDataUri", file.DataUri } }
            );

        }

        [TestMethod]
        public async Task TestSKLuckyNumber()
        {
            var skChatService =
                new ChatClientBuilder(new AnthropicClient().Messages)
                    .UseFunctionInvocation()
                    .Build()
                    .AsChatCompletionService();
            var sk = Kernel.CreateBuilder();
            sk.Plugins.AddFromType<SkPlugins>("LuckyNumber");
            sk.Services.AddSingleton<IChatCompletionService>(skChatService);
            var kernel = sk.Build();
            var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();
            // Create chat history
            var history = new ChatHistory();
            history.AddUserMessage("What is today's lucky number?");
            OpenAIPromptExecutionSettings promptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                ModelId = AnthropicModels.Claude45Haiku,
                MaxTokens = 512
            };
            // Get the response from the AI
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: promptExecutionSettings,
                kernel: kernel
            );
            Assert.IsTrue(result.Content.Contains("895-122"));
        }

        [TestMethod]
        public async Task TestSKLuckyNumberStreaming()
        {
            var skChatService =
                new ChatClientBuilder(new AnthropicClient().Messages)
                    .UseFunctionInvocation()
                    .Build()
                    .AsChatCompletionService();
            var sk = Kernel.CreateBuilder();
            sk.Plugins.AddFromType<SkPlugins>("LuckyNumber");
            sk.Services.AddSingleton<IChatCompletionService>(skChatService);
            var kernel = sk.Build();
            var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();
            // Create chat history
            var history = new ChatHistory();
            history.AddUserMessage("What is today's lucky number?");
            OpenAIPromptExecutionSettings promptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                ModelId = AnthropicModels.Claude45Haiku,
                MaxTokens = 512
            };
            // Get the response from the AI
            var sbResponse = new StringBuilder();
            await foreach (var streamingContent in chatCompletionService.GetStreamingChatMessageContentsAsync(
                               history, promptExecutionSettings, kernel))

            {
                if (streamingContent.Content is not null)
                {
                    sbResponse.Append(streamingContent.Content);
                }
            }

            var result = sbResponse.ToString();

            Assert.Contains("895-122", result);
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
        public Task<string> GetWeather(string location)
        {
            return Task.FromResult("It is 72 degrees and sunny in " + location);
        }

        [KernelFunction("LuckyNumber")]
        [Description("Gets today's lucky number.")]
        public string GetLuckyNumber()
        {
            return "Today's lucky number is 895-122";
        }
    }
}
