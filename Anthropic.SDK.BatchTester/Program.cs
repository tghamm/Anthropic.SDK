using Anthropic.SDK.Batches;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.BatchTester
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Cancel Batch After Creation? (y/n)");
            var cancel = Console.ReadLine() == "y";

            var client = new AnthropicClient();
            Console.WriteLine("Listing Batches...");
            //list batches
            var list = await client.Batches.ListBatchesAsync();
            foreach (var batch in list.Batches)
            {
                Console.WriteLine("Batch: " + batch.Id);
            }

            Console.WriteLine("Creating Batch...");
            var messages = new List<Message>();
            messages.Add(new Message(RoleType.User, "Write me a sonnet about the Statue of Liberty"));
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 512,
                Model = AnthropicModels.Claude35Sonnet,
                Stream = false,
                Temperature = 1.0m,
            };

            var batchRequest = new BatchRequest()
            {
                CustomId = "BatchTester",
                MessageParameters = parameters
            };

            var response = await client.Batches.CreateBatchAsync(new List<BatchRequest> { batchRequest });

            Console.WriteLine("Batch created: " + response.Id);

            if (cancel)
            {
                Console.WriteLine("Cancelling Batch...");
                var cancelResponse = await client.Batches.CancelBatchAsync(response.Id);
                Console.WriteLine("Batch cancelled");
            }
            else
            {
                var processing = true;
                while (processing)
                {
                    var status = await client.Batches.RetrieveBatchStatusAsync(response.Id);
                    Console.WriteLine("Batch status: " + status.ProcessingStatus);
                    if (status.ProcessingStatus == "ended")
                    {
                        processing = false;
                        Console.WriteLine("Batch completed");
                    }
                    else
                    {
                        await Task.Delay(30000);
                    }
                }

                await foreach (var result in client.Batches.RetrieveBatchResultsAsync(response.Id))
                {
                    Console.WriteLine("Result: " + result);
                    messages.Add(new Message(RoleType.Assistant, result.Result.Message.FirstMessage.Text));
                    messages.Add(new Message(RoleType.User, "Who created the Statue of Liberty?"));
                    parameters = new MessageParameters()
                    {
                        Messages = messages,
                        MaxTokens = 512,
                        Model = AnthropicModels.Claude35Sonnet,
                        Stream = false,
                        Temperature = 1.0m,
                    };

                    var clientResponse = await client.Messages.GetClaudeMessageAsync(parameters);
                    Console.WriteLine(clientResponse.FirstMessage.Text);
                }

                await foreach (var result in client.Batches.RetrieveBatchResultsJsonlAsync(response.Id))
                {
                    Console.WriteLine("Result: " + result);
                }
            }
        }
    }
}