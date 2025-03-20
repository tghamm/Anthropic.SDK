using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK.Common;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Vertex AI implementation of the Messages endpoint
    /// </summary>
    public class VertexAIMessagesEndpoint : VertexAIEndpointBase
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

        protected override string Endpoint => "predict";
        
        protected override string Model => _model;

        /// <summary>
        /// Makes a non-streaming call to the Claude messages API via Vertex AI. Be sure to set stream to false in <param name="parameters"></param>.
        /// </summary>
        /// <param name="parameters">The message parameters</param>
        /// <param name="ctx">Cancellation token</param>
        public async Task<MessageResponse> GetClaudeMessageAsync(MessageParameters parameters, CancellationToken ctx = default)
        {
            parameters.Stream = false;
            
            // Convert Anthropic SDK parameters to Vertex AI format
            var vertexRequest = new VertexAIRequest
            {
                Instances = new[]
                {
                    new VertexAIInstance
                    {
                        Messages = parameters.Messages?.ToArray(),
                        System = parameters.System?.FirstOrDefault()?.Text,
                        MaxTokens = parameters.MaxTokens,
                        Temperature = parameters.Temperature.HasValue ? (float?)Convert.ToSingle(parameters.Temperature.Value) : null,
                        TopP = parameters.TopP.HasValue ? (float?)Convert.ToSingle(parameters.TopP.Value) : null,
                        TopK = parameters.TopK,
                        StopSequences = parameters.StopSequences
                    }
                }
            };

            var vertexResponse = await HttpRequestSimple<VertexAIResponse>(Url, HttpMethod.Post, vertexRequest, ctx).ConfigureAwait(false);
            
            // Convert Vertex AI response to Anthropic SDK format
            var response = new MessageResponse
            {
                Id = vertexResponse.Predictions[0].Id,
                Model = _model,
                Type = "message",
                Role = RoleType.Assistant,
                StopReason = vertexResponse.Predictions[0].StopReason,
                StopSequence = vertexResponse.Predictions[0].StopSequence,
                Usage = new Usage
                {
                    InputTokens = vertexResponse.Predictions[0].Usage?.InputTokens ?? 0,
                    OutputTokens = vertexResponse.Predictions[0].Usage?.OutputTokens ?? 0
                }
            };

            // Convert content
            response.Content = vertexResponse.Predictions[0].Content;

            // Handle tool calls if present
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
            parameters.Stream = true;
            
            // Convert Anthropic SDK parameters to Vertex AI format
            var vertexRequest = new VertexAIRequest
            {
                Instances = new[]
                {
                    new VertexAIInstance
                    {
                        Messages = parameters.Messages?.ToArray(),
                        System = parameters.System?.FirstOrDefault()?.Text,
                        MaxTokens = parameters.MaxTokens,
                        Temperature = parameters.Temperature.HasValue ? (float?)Convert.ToSingle(parameters.Temperature.Value) : null,
                        TopP = parameters.TopP.HasValue ? (float?)Convert.ToSingle(parameters.TopP.Value) : null,
                        TopK = parameters.TopK,
                        StopSequences = parameters.StopSequences,
                        Stream = true
                    }
                }
            };

            var toolCalls = new List<Function>();
            var arguments = string.Empty;
            var name = string.Empty;
            bool captureTool = false;
            var id = string.Empty;
            
            await foreach (var result in HttpStreamingRequestMessages(Url, HttpMethod.Post, vertexRequest, ctx).ConfigureAwait(false))
            {
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
        [JsonPropertyName("messages")]
        public Message[] Messages { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public float? TopP { get; set; }

        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }

        [JsonPropertyName("stop_sequences")]
        public string[] StopSequences { get; set; }

        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }
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
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("content")]
        public List<ContentBase> Content { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public string StopSequence { get; set; }

        [JsonPropertyName("usage")]
        public VertexAIUsage Usage { get; set; }
    }

    /// <summary>
    /// Vertex AI usage format
    /// </summary>
    internal class VertexAIUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}