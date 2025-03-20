using System;
using System.Threading.Tasks;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using System.Collections.Generic;

namespace Anthropic.SDK.Tests
{
    /// <summary>
    /// Examples of using the Anthropic SDK with Vertex AI
    /// </summary>
    public class VertexAIExample
    {
        /// <summary>
        /// Basic example of using Vertex AI with Claude
        /// </summary>
        public static async Task VertexAI_BasicExample()
        {
            // Create a Vertex AI client with project ID and region
            var client = new VertexAIClient(
                new VertexAIAuthentication(
                    projectId: "your-google-cloud-project-id",
                    region: "us-central1"
                )
            );

            // Create a message request
            var messages = new List<Message>
            {
                new Message(RoleType.User, "Hello, Claude! Tell me about yourself.")
            };

            // Create message parameters
            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = 1000,
                Temperature = 0.7m
            };

            try
            {
                // Get a response from Claude via Vertex AI
                var response = await client.Messages
                    .WithModel(VertexAIModels.Claude3Sonnet)
                    .GetClaudeMessageAsync(parameters);

                // Print the response
                Console.WriteLine($"Model: {response.Model}");
                Console.WriteLine($"Response: {response.Content[0]}");
                Console.WriteLine($"Usage: {response.Usage.InputTokens} input tokens, {response.Usage.OutputTokens} output tokens");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example of streaming responses from Vertex AI with Claude
        /// </summary>
        public static async Task VertexAI_StreamingExample()
        {
            // Create a Vertex AI client with project ID and region
            var client = new VertexAIClient(
                new VertexAIAuthentication(
                    projectId: "your-google-cloud-project-id",
                    region: "us-central1"
                )
            );

            // Create a message request
            var messages = new List<Message>
            {
                new Message(RoleType.User, "Write a short poem about artificial intelligence.")
            };

            // Create message parameters
            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = 1000,
                Temperature = 0.7m,
                Stream = true
            };

            try
            {
                // Stream a response from Claude via Vertex AI
                Console.WriteLine("Streaming response:");
                
                await foreach (var chunk in client.Messages
                    .WithModel(VertexAIModels.Claude3Haiku)
                    .StreamClaudeMessageAsync(parameters))
                {
                    if (chunk.Delta?.Text != null)
                    {
                        Console.Write(chunk.Delta.Text);
                    }
                }
                
                Console.WriteLine("\nStreaming complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example of listing available Claude models on Vertex AI
        /// </summary>
        public static async Task VertexAI_ListModelsExample()
        {
            // Create a Vertex AI client with project ID and region
            var client = new VertexAIClient(
                new VertexAIAuthentication(
                    projectId: "your-google-cloud-project-id",
                    region: "us-central1"
                )
            );

            try
            {
                // List available Claude models on Vertex AI
                var models = await client.Models.ListModelsAsync();
                
                Console.WriteLine("Available Claude models on Vertex AI:");
                foreach (var model in models.Models)
                {
                    Console.WriteLine($"- {model.DisplayName} ({model.Id})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}