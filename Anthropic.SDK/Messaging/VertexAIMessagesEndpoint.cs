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
            SetCacheControls(parameters);
            
            parameters.Stream = false;
            
            // Create the Vertex AI request
            var vertexRequest = CreateVertexAIRequest(parameters);
            
            var response = await HttpRequestMessages<MessageResponse>(Url, HttpMethod.Post, vertexRequest, ctx).ConfigureAwait(false);
            
            var toolCalls = new List<Function>();
            foreach (var message in response.Content)
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
            response.ToolCalls = toolCalls;
            
            return response;
        }

        /// <summary>
        /// Makes a streaming call to the Claude completion API via Vertex AI using an IAsyncEnumerable. Be sure to set stream to true in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters">The message parameters</param>
        /// <param name="ctx">Cancellation token</param>
        public async IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(MessageParameters parameters, [EnumeratorCancellation] CancellationToken ctx = default)
        {
            SetCacheControls(parameters);
            
            parameters.Stream = true;
            
            // Create the Vertex AI request
            var vertexRequest = CreateVertexAIRequest(parameters);
            
            var toolCalls = new List<Function>();
            var arguments = string.Empty;
            var name = string.Empty;
            bool captureTool = false;
            var id = string.Empty;
            
            await foreach (var result in HttpStreamingRequestMessages(Url, HttpMethod.Post, vertexRequest, ctx).ConfigureAwait(false))
            {
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
        
        private static void SetCacheControls(MessageParameters parameters)
        {
            if (parameters.PromptCaching == PromptCacheType.FineGrained)
            {
                // just use each one's cache control, assume they are already set
            }
            else if (parameters.PromptCaching == PromptCacheType.AutomaticToolsAndSystem)
            {
                if (parameters.System != null && parameters.System.Any())
                {
                    parameters.System.Last().CacheControl = new CacheControl()
                    {
                        Type = CacheControlType.ephemeral
                    };
                }
                
                if (parameters.Tools != null && parameters.Tools.Any())
                {
                    parameters.Tools.Last().Function.CacheControl = new CacheControl()
                    {
                        Type = CacheControlType.ephemeral
                    };
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
            
            return anthropicPayload;
        }

    }
}