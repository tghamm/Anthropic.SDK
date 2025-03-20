using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Anthropic.SDK.Models
{
    /// <summary>
    /// Vertex AI implementation of the Models endpoint
    /// </summary>
    public class VertexAIModelsEndpoint : VertexAIEndpointBase
    {
        /// <summary>
        /// Constructor of the api endpoint. Rather than instantiating this yourself, access it through an instance of <see cref="VertexAIClient"/> as <see cref="VertexAIClient.Models"/>.
        /// </summary>
        /// <param name="client">The Vertex AI client</param>
        internal VertexAIModelsEndpoint(VertexAIClient client) : base(client) { }

        protected override string Endpoint => "models";
        
        protected override string Model => string.Empty;

        /// <summary>
        /// Gets a list of available models on Vertex AI
        /// </summary>
        /// <param name="ctx">Cancellation token</param>
        /// <returns>A list of available models</returns>
        public async Task<ModelList> ListModelsAsync(CancellationToken ctx = default)
        {
            // For Vertex AI, we'll return a static list of available models
            // since the Vertex AI API doesn't have a direct equivalent to Anthropic's list models endpoint
            var models = new List<ModelResponse>
            {
                new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude3Opus,
                    DisplayName = "Claude 3 Opus (Vertex AI)",
                    Type = "model"
                },
                new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude3Sonnet,
                    DisplayName = "Claude 3 Sonnet (Vertex AI)",
                    Type = "model"
                },
                new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude3Haiku,
                    DisplayName = "Claude 3 Haiku (Vertex AI)",
                    Type = "model"
                },
                new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude35Sonnet,
                    DisplayName = "Claude 3.5 Sonnet (Vertex AI)",
                    Type = "model"
                },
                new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude35Haiku,
                    DisplayName = "Claude 3.5 Haiku (Vertex AI)",
                    Type = "model"
                },
                new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude37Sonnet,
                    DisplayName = "Claude 3.7 Sonnet (Vertex AI)",
                    Type = "model"
                }
            };

            return new ModelList { Models = models };
        }

        /// <summary>
        /// Gets information about a specific model
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <param name="ctx">Cancellation token</param>
        /// <returns>Information about the model</returns>
        public async Task<ModelResponse> RetrieveModelAsync(string modelId, CancellationToken ctx = default)
        {
            // For Vertex AI, we'll return information about the requested model from our static list
            ModelResponse model = null;
            
            if (modelId == Constants.VertexAIModels.Claude3Opus)
            {
                model = new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude3Opus,
                    DisplayName = "Claude 3 Opus (Vertex AI)",
                    Type = "model"
                };
            }
            else if (modelId == Constants.VertexAIModels.Claude3Sonnet)
            {
                model = new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude3Sonnet,
                    DisplayName = "Claude 3 Sonnet (Vertex AI)",
                    Type = "model"
                };
            }
            else if (modelId == Constants.VertexAIModels.Claude3Haiku)
            {
                model = new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude3Haiku,
                    DisplayName = "Claude 3 Haiku (Vertex AI)",
                    Type = "model"
                };
            }
            else if (modelId == Constants.VertexAIModels.Claude35Sonnet)
            {
                model = new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude35Sonnet,
                    DisplayName = "Claude 3.5 Sonnet (Vertex AI)",
                    Type = "model"
                };
            }
            else if (modelId == Constants.VertexAIModels.Claude35Haiku)
            {
                model = new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude35Haiku,
                    DisplayName = "Claude 3.5 Haiku (Vertex AI)",
                    Type = "model"
                };
            }
            else if (modelId == Constants.VertexAIModels.Claude37Sonnet)
            {
                model = new ModelResponse
                {
                    Id = Constants.VertexAIModels.Claude37Sonnet,
                    DisplayName = "Claude 3.7 Sonnet (Vertex AI)",
                    Type = "model"
                };
            }
            
            return model;
        }
    }
}