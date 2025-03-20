using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Vertex AI implementation of the Messages endpoint
    /// </summary>
    public partial class VertexAIMessagesEndpoint : VertexAIEndpointBase
    {
        /// <summary>
        /// Constructor of the api endpoint. Rather than instantiating this yourself, access it through an instance of <see cref="VertexAIClient"/> as <see cref="VertexAIClient.Messages"/>.
        /// </summary>
        /// <param name="client">The Vertex AI client</param>
        internal VertexAIMessagesEndpoint(VertexAIClient client) : base(client) { }

        /// <summary>
        /// The current model being used
        /// </summary>
        private string _model = Constants.VertexAIModels.Claude3Sonnet;

        /// <summary>
        /// Sets the model to use for this endpoint
        /// </summary>
        /// <param name="model">The model name</param>
        /// <returns>This endpoint instance for method chaining</returns>
        public VertexAIMessagesEndpoint WithModel(string model)
        {
            _model = model;
            return this;
        }

        protected override string Endpoint => "streamRawPredict";
        
        protected override string Model => _model;

        /// <summary>
        /// Makes a non-streaming call to the Claude messages API via Vertex AI. Be sure to set stream to false in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters">The message parameters</param>
        /// <param name="ctx">Cancellation token</param>
        public async Task<MessageResponse> GetClaudeMessageAsync(MessageParameters parameters, CancellationToken ctx = default)
        {
            parameters.Stream = false;
            
            // Create the Vertex AI request
            var vertexRequest = CreateVertexAIRequest(parameters);
            
            try
            {
                // Make the request using HttpRequestSimple which is accessible
                var jsonResponse = await HttpRequestSimple<JsonElement>(Url, HttpMethod.Post, vertexRequest, ctx).ConfigureAwait(false);
                
                // Debug the response
                Console.WriteLine($"DEBUG - Response: {JsonSerializer.Serialize(jsonResponse)}");
                
                // Create a MessageResponse from the raw response
                var anthropicResponse = new MessageResponse
                {
                    Content = new List<ContentBase>(),
                    Model = Model,
                    Id = Guid.NewGuid().ToString(),
                    Type = "message"
                };
                
                // Extract content from the response
                if (jsonResponse.TryGetProperty("content", out var contentElement))
                {
                    if (contentElement.ValueKind == JsonValueKind.String)
                    {
                        // Simple string content
                        anthropicResponse.Content.Add(new TextContent { Text = contentElement.GetString() });
                    }
                    else if (contentElement.ValueKind == JsonValueKind.Array)
                    {
                        // Array of content blocks
                        foreach (var contentBlock in contentElement.EnumerateArray())
                        {
                            if (contentBlock.TryGetProperty("type", out var typeEl) &&
                                contentBlock.TryGetProperty("text", out var textEl) &&
                                typeEl.GetString() == "text")
                            {
                                anthropicResponse.Content.Add(new TextContent { Text = textEl.GetString() });
                            }
                        }
                    }
                }
                else if (jsonResponse.TryGetProperty("candidates", out var candidatesElement) &&
                         candidatesElement.ValueKind == JsonValueKind.Array &&
                         candidatesElement.GetArrayLength() > 0)
                {
                    var candidate = candidatesElement[0];
                    if (candidate.TryGetProperty("content", out var candidateContent))
                    {
                        if (candidateContent.ValueKind == JsonValueKind.String)
                        {
                            anthropicResponse.Content.Add(new TextContent { Text = candidateContent.GetString() });
                        }
                        else if (candidateContent.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var contentBlock in candidateContent.EnumerateArray())
                            {
                                if (contentBlock.TryGetProperty("type", out var typeEl) &&
                                    contentBlock.TryGetProperty("text", out var textEl) &&
                                    typeEl.GetString() == "text")
                                {
                                    anthropicResponse.Content.Add(new TextContent { Text = textEl.GetString() });
                                }
                            }
                        }
                    }
                }
                
                // Extract additional metadata
                if (jsonResponse.TryGetProperty("role", out var roleElement))
                {
                    // Set the role if present
                    string role = roleElement.GetString();
                    if (role == "assistant")
                    {
                        // Role is already set to assistant by default in MessageResponse
                    }
                }
                
                if (jsonResponse.TryGetProperty("id", out var idElement))
                {
                    anthropicResponse.Id = idElement.GetString();
                }
                
                if (jsonResponse.TryGetProperty("model", out var modelElement))
                {
                    anthropicResponse.Model = modelElement.GetString();
                }
                
                if (jsonResponse.TryGetProperty("stop_reason", out var stopReasonElement))
                {
                    anthropicResponse.StopReason = stopReasonElement.GetString();
                }
                
                if (jsonResponse.TryGetProperty("stop_sequence", out var stopSequenceElement) &&
                    stopSequenceElement.ValueKind != JsonValueKind.Null)
                {
                    anthropicResponse.StopSequence = stopSequenceElement.GetString();
                }
                
                if (jsonResponse.TryGetProperty("usage", out var usageElement))
                {
                    anthropicResponse.Usage = new Usage();
                    
                    if (usageElement.TryGetProperty("input_tokens", out var inputTokensElement))
                    {
                        anthropicResponse.Usage.InputTokens = inputTokensElement.GetInt32();
                    }
                    
                    if (usageElement.TryGetProperty("output_tokens", out var outputTokensElement))
                    {
                        anthropicResponse.Usage.OutputTokens = outputTokensElement.GetInt32();
                    }
                }
                
                // Handle tool calls if present
                var toolCalls = new List<Function>();
                foreach (var message in anthropicResponse.Content)
                {
                    if (message.Type == ContentType.tool_use)
                    {
                        var tool = parameters.Tools?.FirstOrDefault(t => t.Function.Name == (message as ToolUseContent).Name);
                        
                        if (tool != null)
                        {
                            tool.Function.Arguments = (message as ToolUseContent).Input;
                            tool.Function.Id = (message as ToolUseContent).Id;
                            toolCalls.Add(tool.Function);
                        }
                    }
                }
                anthropicResponse.ToolCalls = toolCalls;
                
                return anthropicResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - Failed to get Claude message: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Makes a streaming call to the Claude completion API via Vertex AI using an IAsyncEnumerable. Be sure to set stream to true in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters">The message parameters</param>
        /// <param name="ctx">Cancellation token</param>
        public async IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(MessageParameters parameters, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            parameters.Stream = true;
            
            // Create the Vertex AI request
            var vertexRequest = CreateVertexAIRequest(parameters);
            
            var toolCalls = new List<Function>();
            var arguments = string.Empty;
            var name = string.Empty;
            bool captureTool = false;
            var id = string.Empty;
            string currentContent = string.Empty;
            
            IAsyncEnumerable<string> streamLines;
            
            try
            {
                streamLines = HttpStreamingRequest(Url, HttpMethod.Post, vertexRequest, ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - Failed to initialize stream: {ex.Message}");
                throw;
            }
            
            await foreach (var line in streamLines.ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;
                
                // Extract the data part
                var jsonData = line.Substring(5).Trim();
                if (jsonData == "[DONE]")
                    break;
                
                Console.WriteLine($"DEBUG - Stream data: {jsonData}");
                
                // Try to parse the response
                JsonElement responseElement;
                try
                {
                    responseElement = JsonSerializer.Deserialize<JsonElement>(jsonData);
                }
                catch (JsonException)
                {
                    continue; // Skip malformed JSON
                }
                
                // Create a message response
                var result = new MessageResponse
                {
                    Content = new List<ContentBase>(),
                    Model = Model,
                    Id = Guid.NewGuid().ToString(),
                    Type = "message",
                    Delta = new Delta()
                };
                
                // Extract content from the response
                string deltaText = string.Empty;
                
                if (responseElement.TryGetProperty("content", out var contentElement))
                {
                    if (contentElement.ValueKind == JsonValueKind.String)
                    {
                        deltaText = contentElement.GetString();
                    }
                    else if (contentElement.ValueKind == JsonValueKind.Array)
                    {
                        // Array of content blocks
                        foreach (var contentBlock in contentElement.EnumerateArray())
                        {
                            if (contentBlock.TryGetProperty("type", out var typeEl1) &&
                                contentBlock.TryGetProperty("text", out var textEl1) &&
                                typeEl1.GetString() == "text")
                            {
                                deltaText += textEl1.GetString();
                            }
                        }
                    }
                }
                else if (responseElement.TryGetProperty("delta", out var deltaElement))
                {
                    if (deltaElement.ValueKind == JsonValueKind.String)
                    {
                        deltaText = deltaElement.GetString();
                    }
                    else if (deltaElement.TryGetProperty("text", out var textEl))
                    {
                        deltaText = textEl.GetString();
                    }
                    else if (deltaElement.ValueKind == JsonValueKind.Object &&
                             deltaElement.TryGetProperty("content", out var deltaContent))
                    {
                        if (deltaContent.ValueKind == JsonValueKind.String)
                        {
                            deltaText = deltaContent.GetString();
                        }
                        else if (deltaContent.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var contentBlock in deltaContent.EnumerateArray())
                            {
                                if (contentBlock.TryGetProperty("type", out var typeEl2) &&
                                    contentBlock.TryGetProperty("text", out var textEl2) &&
                                    typeEl2.GetString() == "text")
                                {
                                    deltaText += textEl2.GetString();
                                }
                            }
                        }
                    }
                }
                else if (responseElement.TryGetProperty("candidates", out var candidatesElement) &&
                         candidatesElement.ValueKind == JsonValueKind.Array &&
                         candidatesElement.GetArrayLength() > 0)
                {
                    var candidate = candidatesElement[0];
                    if (candidate.TryGetProperty("content", out var candidateContent))
                    {
                        if (candidateContent.ValueKind == JsonValueKind.String)
                        {
                            deltaText = candidateContent.GetString();
                        }
                        else if (candidateContent.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var contentBlock in candidateContent.EnumerateArray())
                            {
                                if (contentBlock.TryGetProperty("type", out var typeEl3) &&
                                    contentBlock.TryGetProperty("text", out var textEl3) &&
                                    typeEl3.GetString() == "text")
                                {
                                    deltaText += textEl3.GetString();
                                }
                            }
                        }
                    }
                }
                
                // Extract additional metadata
                if (responseElement.TryGetProperty("id", out var idElement))
                {
                    result.Id = idElement.GetString();
                }
                
                if (responseElement.TryGetProperty("model", out var modelElement))
                {
                    result.Model = modelElement.GetString();
                }
                
                if (responseElement.TryGetProperty("stop_reason", out var stopReasonElement))
                {
                    result.StopReason = stopReasonElement.GetString();
                    result.Delta.StopReason = stopReasonElement.GetString();
                }
                
                if (!string.IsNullOrEmpty(deltaText))
                {
                    currentContent += deltaText;
                    result.Delta.Text = deltaText;
                    result.Content.Add(new TextContent { Text = currentContent });
                }
                
                // Handle tool calls if present
                if (result.ContentBlock != null && result.ContentBlock.Type == "tool_use")
                {
                    arguments = string.Empty;
                    captureTool = true;
                    name = result.ContentBlock.Name;
                    id = result.ContentBlock.Id;
                }
                
                if (!string.IsNullOrWhiteSpace(result.Delta?.PartialJson))
                {
                    arguments += result.Delta.PartialJson;
                }
                
                if (captureTool && result.Delta?.StopReason == "tool_use")
                {
                    var tool = parameters.Tools?.FirstOrDefault(t => t.Function.Name == name);
                    
                    if (tool != null)
                    {
                        tool.Function.Arguments = arguments;
                        tool.Function.Id = id;
                        toolCalls.Add(tool.Function);
                    }
                    captureTool = false;
                    result.ToolCalls = toolCalls;
                }
                
                yield return result;
            }
        }

        /// <summary>
        /// Converts Anthropic content to the format expected by Claude
        /// </summary>
        private object ConvertMessageContent(List<ContentBase> content)
        {
            // For simple text content, just return the text
            if (content.Count == 1 && content[0] is TextContent textContent)
            {
                return textContent.Text;
            }
            
            // For more complex content, convert to appropriate format
            var result = new List<object>();
            
            foreach (var c in content)
            {
                if (c is TextContent tc)
                {
                    result.Add(new { type = "text", text = tc.Text });
                }
                else if (c is ImageContent ic)
                {
                    if (ic.Source.Type == SourceType.url)
                    {
                        result.Add(new { type = "image", source = new { type = "url", url = ic.Source.Url } });
                    }
                    else if (ic.Source.Type == SourceType.base64)
                    {
                        result.Add(new { type = "image", source = new { type = "base64", data = ic.Source.Data, media_type = ic.Source.MediaType } });
                    }
                }
                else if (c is ToolUseContent tuc)
                {
                    result.Add(new { type = "tool_use", id = tuc.Id, name = tuc.Name, input = tuc.Input });
                }
                else if (c is ToolResultContent trc)
                {
                    result.Add(new { type = "tool_result", tool_use_id = trc.ToolUseId, content = trc.Content });
                }
                else
                {
                    // Default fallback
                    result.Add(new { type = "text", text = c.ToString() });
                }
            }
            
            return result.ToArray();
        }

        /// <summary>
        /// Creates a Vertex AI request from Anthropic message parameters
        /// </summary>
        private object CreateVertexAIRequest(MessageParameters parameters)
        {
            // Create direct request structure based on Vertex AI documentation
            var request = new
            {
                anthropic_version = "vertex-2023-10-16",
                messages = parameters.Messages?.Select(m => new
                {
                    role = m.Role.ToString().ToLower(),
                    content = ConvertMessageContent(m.Content)
                }).ToArray(),
                system = parameters.System?.FirstOrDefault()?.Text,
                max_tokens = parameters.MaxTokens,
                temperature = parameters.Temperature,
                top_p = parameters.TopP,
                top_k = parameters.TopK,
                stop_sequences = parameters.StopSequences,
                stream = parameters.Stream,
                tools = parameters.Tools?.Select(t => new
                {
                    function = new
                    {
                        name = t.Function.Name,
                        description = t.Function.Description,
                        parameters = t.Function.Parameters
                    }
                }).ToArray(),
                tool_choice = parameters.ToolChoice != null ? new
                {
                    type = parameters.ToolChoice.Type.ToString().ToLower(),
                    name = parameters.ToolChoice.Name
                } : null,
                thinking = parameters.Thinking != null ? new
                {
                    type = parameters.Thinking.Type.ToString().ToLower(),
                    budget_tokens = parameters.Thinking.BudgetTokens
                } : null
            };

            // For debugging
            Console.WriteLine($"DEBUG - Request structure: {JsonSerializer.Serialize(request)}");
            
            return request;
        }

        /// <summary>
        /// Makes a streaming HTTP request and returns the response as a stream of lines
        /// </summary>
        private async IAsyncEnumerable<string> HttpStreamingRequest(string url, HttpMethod method, object data, [EnumeratorCancellation] CancellationToken ctx)
        {
            var request = new HttpRequestMessage(method, url);
            var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            var response = await GetClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx);
            response.EnsureSuccessStatusCode();
            
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);
            
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                yield return line;
            }
        }
    }

    /// <summary>
    /// Vertex AI request format
    /// </summary>
    internal class VertexAIRequest
    {
        [JsonPropertyName("instances")]
        public VertexAIInstance[] Instances { get; set; }
    }

    /// <summary>
    /// Vertex AI instance format
    /// </summary>
    internal class VertexAIInstance
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        [JsonPropertyName("messages")]
        public VertexAIMessage[] Messages { get; set; }
        
        [JsonPropertyName("system")]
        public string System { get; set; }
        
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }
        
        [JsonPropertyName("temperature")]
        public decimal? Temperature { get; set; }
        
        [JsonPropertyName("top_p")]
        public decimal? TopP { get; set; }
        
        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }
        
        [JsonPropertyName("stop_sequences")]
        public string[] StopSequences { get; set; }
        
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }
        
        [JsonPropertyName("tools")]
        public VertexAITool[] Tools { get; set; }
        
        [JsonPropertyName("tool_choice")]
        public VertexAIToolChoice ToolChoice { get; set; }
    }

    /// <summary>
    /// Vertex AI response format
    /// </summary>
    internal class VertexAIResponse
    {
        [JsonPropertyName("predictions")]
        public VertexAIPrediction[] Predictions { get; set; }
    }

    /// <summary>
    /// Vertex AI prediction format
    /// </summary>
    internal class VertexAIPrediction
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    /// <summary>
    /// Vertex AI streaming response format
    /// </summary>
    internal class VertexAIStreamResponse
    {
        [JsonPropertyName("predictions")]
        public VertexAIPrediction[] Predictions { get; set; }
    }

    /// <summary>
    /// Vertex AI message format for Claude
    /// </summary>
    internal class VertexAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public object Content { get; set; }
    }

    /// <summary>
    /// Vertex AI tool format for Claude
    /// </summary>
    internal class VertexAITool
    {
        [JsonPropertyName("function")]
        public VertexAIFunction Function { get; set; }
    }

    /// <summary>
    /// Vertex AI function format for Claude
    /// </summary>
    internal class VertexAIFunction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parameters")]
        public object Parameters { get; set; }
    }

    /// <summary>
    /// Vertex AI tool choice format for Claude
    /// </summary>
    internal class VertexAIToolChoice
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}