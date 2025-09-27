using System.Reflection;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class DocumentTests
    {
        [TestMethod]
        public async Task TestPDF()
        {
            string resourceName = "Anthropic.SDK.Tests.Claude3ModelCard.pdf";

            Assembly assembly = Assembly.GetExecutingAssembly();

            await using Stream stream = assembly.GetManifestResourceStream(resourceName);
            //read stream into byte array
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            byte[] pdfBytes = ms.ToArray();
            string base64String = Convert.ToBase64String(pdfBytes);

            
            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new Message(RoleType.User, new DocumentContent()
                {
                    Source = new DocumentSource()
                    {
                        Type = SourceType.base64,
                        Data = base64String,
                        MediaType = "application/pdf"
                    },
                    CacheControl = new CacheControl()
                    {
                        Type = CacheControlType.ephemeral,
                        TTL = CacheDuration.FiveMinutes,
                    }
                }),
                new Message(RoleType.User, "Which model has the highest human preference win rates across each use-case?"),
            };
            
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 0m,
                PromptCaching = PromptCacheType.FineGrained
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);
            
            Assert.IsNotNull(res.FirstMessage.ToString());
            Assert.IsTrue(res.Usage.CacheCreationInputTokens > 0 || res.Usage.CacheReadInputTokens > 0);
            if (res.Usage.CacheCreationInputTokens > 0)
            {
                Assert.IsNotNull(res.Usage.CacheCreation);
                Assert.IsTrue(res.Usage.CacheCreation.Ephemeral5mInputTokens > 0);
            }
        }

        [TestMethod]
        public async Task TestPDFCitations()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new Message(RoleType.User, new DocumentContent()
                {
                    Source = new DocumentSource()
                    {
                        Type = SourceType.url,
                        Url = "https://assets.anthropic.com/m/1cd9d098ac3e6467/original/Claude-3-Model-Card-October-Addendum.pdf"
                    },
                    CacheControl = new CacheControl()
                    {
                        Type = CacheControlType.ephemeral
                    },
                    Citations = new Citations() { Enabled = true }
                }),
                new Message(RoleType.User, "What are the key findings in this document? Use citations to back up your answer."),
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 0m,
                PromptCaching = PromptCacheType.FineGrained
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res.Content.SelectMany(p => (p as TextContent).Citations ?? new List<CitationResult>()).Any());
            

        }

        [TestMethod]
        public async Task TestPDFCitationsStreaming()
        {
            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new Message(RoleType.User, new DocumentContent()
                {
                    Source = new DocumentSource()
                    {
                        Type = SourceType.url,
                        Url = "https://assets.anthropic.com/m/1cd9d098ac3e6467/original/Claude-3-Model-Card-October-Addendum.pdf"
                    },
                    CacheControl = new CacheControl()
                    {
                        Type = CacheControlType.ephemeral
                    },
                    Citations = new Citations() { Enabled = true }
                }),
                new Message(RoleType.User, "What are the key findings in this document? Use citations to back up your answer."),
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 0m,
                PromptCaching = PromptCacheType.FineGrained
            };
            var responses = new List<MessageResponse>();
            await foreach (var result in client.Messages.StreamClaudeMessageAsync(parameters))
            {
                responses.Add(result);
            }

            var message = new Message(responses);
            Assert.IsTrue(message.Content.SelectMany(p => (p as TextContent).Citations ?? new List<CitationResult>()).Any());

        }

        [TestMethod]
        public async Task TestDocumentCitations()
        {
            string resourceName = "Anthropic.SDK.Tests.BillyBudd.txt";

            Assembly assembly = Assembly.GetExecutingAssembly();

            await using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            string content = await reader.ReadToEndAsync();


            var client = new AnthropicClient();
            var messages = new List<Message>()
            {
                new Message(RoleType.User, new DocumentContent()
                {
                    Source = new DocumentSource()
                    {
                        Type = SourceType.content,
                        Content = [new TextContent()
                        {
                            Text = content
                        }]
                    },
                    Citations = new Citations() { Enabled = true }
                }),
                new Message(RoleType.User, "Who is the protagonist in this text? Use citations to back up your answer."),
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1024,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 0m
            };
            var res = await client.Messages.GetClaudeMessageAsync(parameters);

            Assert.IsTrue(res.Content.SelectMany(p => (p as TextContent).Citations ?? new List<CitationResult>()).Any());


        }


    }


}
