using System;
using System.Collections.Generic;
using System.IO;
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
            
            // Process SSE events
            SseEvent currentEvent = new SseEvent();
            
            await foreach (var line in streamLines.ConfigureAwait(false))
            {
                if (string.IsNullOrEmpty(line))
                {
                    // Empty line indicates the end of an event
                    if (!string.IsNullOrEmpty(currentEvent.Data))
                    {
                        if (currentEvent.Data == "[DONE]")
                            break;
                        
                        Console.WriteLine($"DEBUG - SSE event data: {currentEvent.Data}");
                        
                        // Process the event data
                        MessageResponse result = null;
                        
                        // First try to parse as a standard MessageResponse
                        try
                        {
                            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(currentEvent.Data));
                            result = await JsonSerializer.DeserializeAsync<MessageResponse>(ms, cancellationToken: ctx).ConfigureAwait(false);
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"ERROR - Failed to parse as MessageResponse: {ex.Message}");
                            
                            // Try to parse as a Vertex AI response
                            try
                            {
                                var vertexResponse = JsonSerializer.Deserialize<JsonElement>(currentEvent.Data);
                                
                                // Check if it has predictions
                                if (vertexResponse.TryGetProperty("predictions", out var predictions) &&
                                    predictions.ValueKind == JsonValueKind.Array &&
                                    predictions.GetArrayLength() > 0)
                                {
                                    var prediction = predictions[0];
                                    string content = string.Empty;
                                    
                                    // Try to get content as string
                                    if (prediction.ValueKind == JsonValueKind.String)
                                    {
                                        content = prediction.GetString();
                                    }
                                    else if (prediction.TryGetProperty("content", out var contentElement))
                                    {
                                        content = contentElement.GetString();
                                    }
                                    
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        // Create a simple message response
                                        result = new MessageResponse
                                        {
                                            Content = new List<ContentBase> { new TextContent { Text = content } },
                                            Model = Model,
                                            Id = Guid.NewGuid().ToString(),
                                            Type = "message"
                                        };
                                    }
                                }
                            }
                            catch (JsonException innerEx)
                            {
                                Console.WriteLine($"ERROR - Failed to parse as Vertex AI response: {innerEx.Message}");
                                // If we can't parse as JSON at all, just continue
                            }
                        }
                        
                        // Process the result if we have one
                        if (result != null)
                        {
                            // Set model if not already set
                            if (string.IsNullOrEmpty(result.Model))
                            {
                                result.Model = Model;
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
                            
                            // If we have delta text, update the content
                            if (!string.IsNullOrEmpty(result.Delta?.Text))
                            {
                                currentContent += result.Delta.Text;
                                
                                // Make sure we have a content list
                                if (result.Content == null)
                                {
                                    result.Content = new List<ContentBase>();
                                }
                                
                                // Update or add the text content
                                bool foundTextContent = false;
                                foreach (var content in result.Content)
                                {
                                    if (content is TextContent textContent)
                                    {
                                        textContent.Text = currentContent;
                                        foundTextContent = true;
                                        break;
                                    }
                                }
                                
                                if (!foundTextContent)
                                {
                                    result.Content.Add(new TextContent { Text = currentContent });
                                }
                            }
                            
                            yield return result;
                        }
                    }
                    
                    // Reset the event
                    currentEvent = new SseEvent();
                }
                else if (line.StartsWith("event:"))
                {
                    currentEvent.EventType = line.Substring("event:".Length).Trim();
                }
                else if (line.StartsWith("data:"))
                {
                    currentEvent.Data = line.Substring("data:".Length).Trim();
                }
            }
        }
        
        /// <summary>
        /// Helper method to extract content from various possible response formats
        /// </summary>
        private bool TryExtractContent(JsonElement responseElement, out string deltaText)
        {
            deltaText = string.Empty;
            
            // Try to extract from direct content property
            if (responseElement.TryGetProperty("content", out var contentElement))
            {
                if (contentElement.ValueKind == JsonValueKind.String)
                {
                    deltaText = contentElement.GetString();
                    return true;
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
                    return !string.IsNullOrEmpty(deltaText);
                }
            }
            
            // Try to extract from delta property
            if (responseElement.TryGetProperty("delta", out var deltaElement))
            {
                if (deltaElement.ValueKind == JsonValueKind.String)
                {
                    deltaText = deltaElement.GetString();
                    return true;
                }
                else if (deltaElement.TryGetProperty("text", out var textEl))
                {
                    deltaText = textEl.GetString();
                    return true;
                }
                else if (deltaElement.ValueKind == JsonValueKind.Object &&
                         deltaElement.TryGetProperty("content", out var deltaContent))
                {
                    if (deltaContent.ValueKind == JsonValueKind.String)
                    {
                        deltaText = deltaContent.GetString();
                        return true;
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
                        return !string.IsNullOrEmpty(deltaText);
                    }
                }
            }
            
            // Try to extract from candidates property (Vertex AI format)
            if (responseElement.TryGetProperty("candidates", out var candidatesElement) &&
                candidatesElement.ValueKind == JsonValueKind.Array &&
                candidatesElement.GetArrayLength() > 0)
            {
                var candidate = candidatesElement[0];
                if (candidate.TryGetProperty("content", out var candidateContent))
                {
                    if (candidateContent.ValueKind == JsonValueKind.String)
                    {
                        deltaText = candidateContent.GetString();
                        return true;
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
                        return !string.IsNullOrEmpty(deltaText);
                    }
                }
            }
            
            return false;
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
            // Create the Anthropic-specific payload - same for both streaming and non-streaming
            var anthropicPayload = new
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
            Console.WriteLine($"DEBUG - Request structure: {JsonSerializer.Serialize(anthropicPayload)}");
            
            return anthropicPayload;
        }

        /// <summary>
        /// Makes a streaming HTTP request and returns the response as a stream of SSE events
        /// </summary>
        private async IAsyncEnumerable<string> HttpStreamingRequest(string url, HttpMethod method, object data, [EnumeratorCancellation] CancellationToken ctx)
        {
            var request = new HttpRequestMessage(method, url);
            var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            // Add specific headers for streaming
            request.Headers.Add("Accept", "text/event-stream");
            
            HttpResponseMessage response = null;
            Stream stream = null;
            StreamReader reader = null;
            
            try
            {
                response = await GetClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx);
                response.EnsureSuccessStatusCode();
                
                stream = await response.Content.ReadAsStreamAsync();
                reader = new StreamReader(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - Streaming request failed: {ex.Message}");
                if (response != null)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"ERROR - Response content: {errorContent}");
                }
                throw;
            }
            
            try
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // Debug the raw response line
                    Console.WriteLine($"DEBUG - Raw stream line: {line}");
                    
                    if (string.IsNullOrEmpty(line))
                        continue;
                    
                    // Pass through the SSE line directly
                    yield return line;
                }
            }
            finally
            {
                reader?.Dispose();
                stream?.Dispose();
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