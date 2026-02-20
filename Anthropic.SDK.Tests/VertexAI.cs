using System.Diagnostics;
using System.Reflection;
using System.Text;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class VertexAI
    {
        // Load settings from appsettings.json
        private static readonly TestSettings Settings = TestSettings.LoadSettings();
        private static readonly string TestProjectId = Settings.VertexAIProjectId;
        private static readonly string TestRegion = Settings.VertexAIRegion;
        private static readonly string TestAccessToken = Settings.VertexAIAccessToken;

        [TestMethod]
        public async Task TestBasicVertexAIMessage()
        {
            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion, accessToken: TestAccessToken));
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty. The response must include the word green."));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = Constants.VertexAIModels.Claude46Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };
            
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            // Convert the content to get the text response
            var textContent = res.Content.OfType<TextContent>().FirstOrDefault();
            Assert.IsNotNull(textContent, "No text content found in response");
            
            // Assert that the response contains specific content we asked for
            Assert.IsTrue(textContent.Text.Contains("green"), textContent.Text);
        }

        [TestMethod]
        public async Task TestVertexAIWithModelSelection()
        {
            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion, accessToken: TestAccessToken));
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "How many r's are in the word strawberry?"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Stream = false,
                Temperature = 1.0m,
                Model = Constants.VertexAIModels.Claude46Sonnet
            };
            
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            // Convert the content to get the text response
            var textContent = res.Content.OfType<TextContent>().FirstOrDefault();
            Assert.IsNotNull(textContent, "No text content found in response");
            
            // Assert that the response contains the number 3 (correct answer)
            Assert.IsTrue(textContent.Text.Contains("3"), textContent.Text);
        }

        [TestMethod]
        public async Task TestStreamingVertexAIMessage()
        {
            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion, accessToken: TestAccessToken));
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "How many r's are in the word strawberry?"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = Constants.VertexAIModels.Claude46Sonnet,
                Stream = true,
                Temperature = 1.0m,
            };
            
            // Collect all streamed responses
            var outputs = new List<MessageResponse>();
            StringBuilder sb = new();
            
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    sb.Append(res.Delta.Text);
                }
                outputs.Add(res);
            }
            
            // Get the combined output from all stream chunks
            string fullResponse = sb.ToString();
            
            // Verify that the response contains the correct answer
            Assert.IsTrue(fullResponse.Contains("3"), fullResponse);
        }

        [TestMethod]
        public async Task TestVertexAIImageMessage()
        {
            string resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

            Assembly assembly = Assembly.GetExecutingAssembly();

            await using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }
            
            string base64String = Convert.ToBase64String(imageBytes);

            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion, accessToken: TestAccessToken));
            
            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = new List<ContentBase>()
                {
                    new ImageContent()
                    {
                        Source = new ImageSource()
                        {
                            MediaType = "image/jpeg",
                            Data = base64String
                        }
                    },
                    new TextContent()
                    {
                        Text = "What is this a picture of?"
                    }
                }
            });
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = Constants.VertexAIModels.Claude46Sonnet,
                Stream = false,
                Temperature = 0.0m, // Use deterministic output for testing
            };
            
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            // Convert the content to get the text response
            var textContent = res.Content.OfType<TextContent>().FirstOrDefault();
            Assert.IsNotNull(textContent, "No text content found in response");
            
            // Assert that the response correctly identifies an apple
            Assert.IsTrue(textContent.Text.Contains("apple", StringComparison.OrdinalIgnoreCase), textContent.Text);
        }

        [TestMethod]
        public async Task TestStreamingVertexAIImageMessage()
        {
            string resourceName = "Anthropic.SDK.Tests.Red_Apple.jpg";

            Assembly assembly = Assembly.GetExecutingAssembly();

            await using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            string base64String = Convert.ToBase64String(imageBytes);

            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion, accessToken: TestAccessToken));
            var messages = new List<Message>();
            messages.Add(new Message()
            {
                Role = RoleType.User,
                Content = new List<ContentBase>()
                {
                    new ImageContent()
                    {
                        Source = new ImageSource()
                        {
                            MediaType = "image/jpeg",
                            Data = base64String
                        }
                    },
                    new TextContent()
                    {
                        Text = "What is this a picture of?"
                    }
                }
            });
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = Constants.VertexAIModels.Claude46Sonnet,
                Stream = true,
                Temperature = 0.0m, // Use deterministic output for testing
            };
            
            // Collect all streamed responses
            var outputs = new List<MessageResponse>();
            StringBuilder sb = new();
            
            await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    sb.Append(res.Delta.Text);
                }
                outputs.Add(res);
            }
            
            // Get the combined output from all stream chunks
            string fullResponse = sb.ToString();
            
            // Verify that the response correctly identifies an apple
            Assert.IsTrue(fullResponse.Contains("apple", StringComparison.OrdinalIgnoreCase), fullResponse);
        }

        [TestMethod]
        public async Task TestVertexAIWithSingleSystemPrompt()
        {
            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion, accessToken: TestAccessToken));
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "What color is an apple?"));
            
            var systemPrompt = new SystemMessage("You must always answer in rhyming verse.");
            
            var parameters = new MessageParameters()
            {
                Messages = messages,
                System = new List<SystemMessage> { systemPrompt },
                MaxTokens = 512,
                Model = Constants.VertexAIModels.Claude46Sonnet,
                Stream = false,
                Temperature = 0.7m,
            };
            
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            // Convert the content to get the text response
            var textContent = res.Content.OfType<TextContent>().FirstOrDefault();
            Assert.IsNotNull(textContent, "No text content found in response");
            
            // The response should be in rhyming verse as instructed by the system prompt
            string response = textContent.Text;
            Debug.WriteLine(response);
            
            // Check for verse format (lines that end with similar sounds)
            // This is a simple test - we're looking for line breaks as an indicator of verse structure
            Assert.IsTrue(response.Contains("\n"), "Response should be formatted as verse with line breaks");
            
            // We could add more sophisticated checks for rhyming patterns, but that would be complex
            // For this test, we're primarily checking that the system prompt was processed
        }

        [TestMethod]
        public async Task TestVertexAIWithMultipleSystemPrompts()
        {
            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion, accessToken: TestAccessToken));
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Describe a beach scene."));
            
            // Multiple system prompts with different instructions
            var systemPrompts = new List<SystemMessage>
            {
                new SystemMessage("You must always answer in rhyming verse."),
                new SystemMessage("Your descriptions must include the color blue.")
            };
            
            var parameters = new MessageParameters()
            {
                Messages = messages,
                System = systemPrompts,
                MaxTokens = 512,
                Model = Constants.VertexAIModels.Claude46Sonnet,
                Stream = false,
                Temperature = 0.7m,
            };
            
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            // Convert the content to get the text response
            var textContent = res.Content.OfType<TextContent>().FirstOrDefault();
            Assert.IsNotNull(textContent, "No text content found in response");
            
            // The response should be in rhyming verse AND mention the color blue
            string response = textContent.Text;
            Debug.WriteLine(response);
            
            // Check for verse format
            Assert.IsTrue(response.Contains("\n"), "Response should be formatted as verse with line breaks");
            
            // Check that the color blue is mentioned as required by the second system prompt
            Assert.IsTrue(response.Contains("blue", StringComparison.OrdinalIgnoreCase),
                "Response should mention the color blue as specified in the system prompt");
        }

        [TestMethod]
        public async Task TestVertexAIWithSystemPromptCacheControl()
        {
            var client = new VertexAIClient(new VertexAIAuthentication(TestProjectId, TestRegion, accessToken: TestAccessToken));
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Summarize the main character's traits."));
            
            // Create system prompts with cache control
            var systemPrompts = new List<SystemMessage>
            {
                new SystemMessage("You are an AI assistant tasked with analyzing literary works. Your goal is to provide insightful commentary on themes, characters, and writing style."),
                new SystemMessage("The main character is a proud, intelligent woman who initially judges people too quickly but learns humility and understanding through her experiences.",
                    new CacheControl { Type = CacheControlType.ephemeral })
            };
            
            var parameters = new MessageParameters()
            {
                Messages = messages,
                System = systemPrompts,
                MaxTokens = 512,
                Model = Constants.VertexAIModels.Claude46Sonnet,
                Stream = false,
                Temperature = 0.7m,
                PromptCaching = PromptCacheType.FineGrained // Use fine-grained caching since we're setting cache control explicitly
            };
            
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            // Convert the content to get the text response
            var textContent = res.Content.OfType<TextContent>().FirstOrDefault();
            Assert.IsNotNull(textContent, "No text content found in response");
            
            // The response should contain insights about the character traits
            string response = textContent.Text;
            Debug.WriteLine(response);
            
            // Check that the response mentions character traits from the system message
            Assert.IsTrue(response.Contains("proud", StringComparison.OrdinalIgnoreCase) ||
                         response.Contains("intelligent", StringComparison.OrdinalIgnoreCase) ||
                         response.Contains("judg", StringComparison.OrdinalIgnoreCase) ||
                         response.Contains("humilit", StringComparison.OrdinalIgnoreCase),
                "Response should mention character traits from the system message");
        }
    }
}