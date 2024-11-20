using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class PDFTests
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
                    Source = new ImageSource()
                    {
                        Data = base64String,
                        MediaType = "application/pdf"
                    },
                    CacheControl = new CacheControl()
                    {
                        Type = CacheControlType.ephemeral
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

            Debug.WriteLine(res.Message);
            Assert.IsTrue(res.Usage.CacheCreationInputTokens > 0 || res.Usage.CacheReadInputTokens > 0);

            
        }
    }
}
