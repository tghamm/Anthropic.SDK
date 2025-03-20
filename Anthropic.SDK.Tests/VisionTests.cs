using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Tests
{
    [TestClass]
    public class VisionTests
    {
        [TestMethod]
        public async Task TestVisionUrl()
        {
            var client = new AnthropicClient();

            var mp = new MessageParameters()
            {
                Model = AnthropicModels.Claude37Sonnet,
                MaxTokens = 1024,
                Messages = new List<Message>()
                {
                    new Message()
                    {
                        Content = new List<ContentBase>()
                        {
                            new ImageContent()
                            {
                                Source = new ImageSource()
                                {
                                    Type = SourceType.url,
                                    Url =
                                        "https://upload.wikimedia.org/wikipedia/commons/a/a7/Camponotus_flavomarginatus_ant.jpg"
                                }
                            },
                            new TextContent()
                            {
                                Text = "Describe this image."
                            }
                        }
                    }
                }


            };
            var res = await client.Messages.GetClaudeMessageAsync(mp);
            Assert.IsNotNull(res.FirstMessage.ToString());
        }
    }
}
