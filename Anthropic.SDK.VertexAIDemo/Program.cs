using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.VertexAIDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Anthropic SDK - Vertex AI Demo");
            Console.WriteLine("==============================");
            
            Console.WriteLine("Checking for gcloud CLI authentication...");
            bool isGcloudAuthenticated = false;
            string gcloudAccessToken = null;
            
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "gcloud",
                        Arguments = "auth print-access-token",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                gcloudAccessToken = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(gcloudAccessToken) && !gcloudAccessToken.Contains("ERROR"))
                {
                    isGcloudAuthenticated = true;
                    Console.WriteLine("Found existing gcloud CLI authentication.");
                }
            }
            catch
            {
                Console.WriteLine("gcloud CLI not found or not authenticated.");
            }
            
            // Get Google Cloud project ID and region from environment variables or command line
            string projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")
                ?? GetInput("Enter your Google Cloud Project ID: ");
            
            string region = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_REGION")
                ?? GetInput("Enter your Google Cloud Region (e.g., us-east5): ");
            
            // Create a Vertex AI client
            VertexAIClient client;
            
            if (isGcloudAuthenticated)
            {
                // Use gcloud CLI authentication
                client = new VertexAIClient(
                    new VertexAIAuthentication(projectId, region, accessToken: gcloudAccessToken)
                );
                Console.WriteLine("Using gcloud CLI authentication.");
            }
            else
            {
                // Use default authentication (will try to use gcloud CLI in the background)
                client = new VertexAIClient(
                    new VertexAIAuthentication(projectId, region)
                );
                Console.WriteLine("Using default authentication mechanism.");
            }
            
            // Get user input for a message to Claude
            Console.WriteLine("\nSend a message to Claude via Vertex AI");
            string userMessage = GetInput("Enter your message: ");
            
            // Create message parameters
            var messages = new List<Message>
            {
                new Message(RoleType.User, userMessage)
            };
            
            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = 1000,
                Temperature = 0.7m,
                Model = VertexAIModels.Claude37Sonnet
            };
            
            // Ask if user wants streaming or non-streaming
            Console.WriteLine("\nDo you want to stream the response?");
            bool useStreaming = GetYesNoInput("Stream response (y/n): ");
            
            try
            {
                if (useStreaming)
                {
                    // Stream the response
                    Console.WriteLine("\nStreaming response from Claude via Vertex AI...\n");
                    parameters.Stream = true;
                    
                    Console.WriteLine("Debug output will be shown in [DEBUG] blocks");
                    Console.WriteLine("Actual response content will be shown directly\n");
                    
                    // Add a console trace listener to capture debug output
                    System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
                    string fullResponse = "";
                    await foreach (var chunk in client.Messages
                        .StreamClaudeMessageAsync(parameters))
                    {
                        if (chunk.Delta?.Text != null)
                        {
                            Console.Write(chunk.Delta.Text);
                            fullResponse += chunk.Delta.Text;
                        }
                    }
                    
                    Console.WriteLine("\n\nFull response:");
                    Console.WriteLine(fullResponse);
                    Console.WriteLine("\n");
                }
                else
                {
                    // Get a non-streaming response
                    Console.WriteLine("\nGetting response from Claude via Vertex AI...\n");
                    var response = await client.Messages
                        .GetClaudeMessageAsync(parameters);
                    
                    Console.WriteLine($"Response: {response.Content[0]}");
                    Console.WriteLine($"\nUsage: {response.Usage.InputTokens} input tokens, {response.Usage.OutputTokens} output tokens");
                }
                
                Console.WriteLine("\nDemo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static string GetInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }
        
        static bool GetYesNoInput(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine().Trim().ToLower();
                
                if (input == "y" || input == "yes")
                    return true;
                if (input == "n" || input == "no")
                    return false;
                
                Console.WriteLine("Please enter 'y' or 'n'.");
            }
        }
    }
}