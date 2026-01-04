using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Anthropic.SDK.Common;
using Anthropic.SDK.Extensions;
using Microsoft.Extensions.AI;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Helper class for chat client implementations
    /// </summary>
    public static class ChatClientHelper
    {
        /// <summary>
        /// Create usage details from usage
        /// </summary>
        public static UsageDetails CreateUsageDetails(Usage usage) =>
            new()
            {
                InputTokenCount = usage.InputTokens,
                OutputTokenCount = usage.OutputTokens,
                AdditionalCounts = new()
                {
                    [nameof(usage.CacheCreationInputTokens)] = usage.CacheCreationInputTokens,
                    [nameof(usage.CacheReadInputTokens)] = usage.CacheReadInputTokens,
                }
            };

        /// <summary>
        /// Create message parameters from chat messages and options
        /// </summary>
        public static MessageParameters CreateMessageParameters(IChatClient client, IEnumerable<ChatMessage> messages, ChatOptions options)
        {
            MessageParameters parameters = options?.RawRepresentationFactory?.Invoke(client) as MessageParameters ?? new();

            parameters.Messages ??= [];
            if (parameters.MaxTokens == 0)
            {
                // Not setting MaxTokens to a value > 0 results in error.
                parameters.MaxTokens = 4096;
            }

            if (options is not null)
            {
                parameters.Model = options.ModelId;

                if (options.Instructions is string instructions)
                {
                    (parameters.System ??= []).Add(new SystemMessage(instructions));
                }

                if (options.MaxOutputTokens is int maxOutputTokens)
                {
                    parameters.MaxTokens = maxOutputTokens;
                }

                if (options.Temperature is float temperature)
                {
                    parameters.Temperature = (decimal)temperature;
                }

                if (options.TopP is float topP)
                {
                    parameters.TopP = (decimal)topP;
                }

                if (options.TopK is int topK)
                {
                    parameters.TopK = topK;
                }

                if (options.StopSequences is not null)
                {
                    parameters.StopSequences = options.StopSequences.ToArray();
                }

                // Determine if strict mode should be enabled for tools (when ResponseFormat has a schema)
                bool useStrictTools = options.ResponseFormat is ChatResponseFormatJson { Schema: not null } ||
                                      options.GetStrictToolsEnabled();

                if (options.Tools is { Count: > 0 })
                {
                    parameters.ToolChoice ??= new();

                    if (options.ToolMode is RequiredChatToolMode r)
                    {
                        parameters.ToolChoice.Type = r.RequiredFunctionName is null ? ToolChoiceType.Any : ToolChoiceType.Tool;
                        parameters.ToolChoice.Name = r.RequiredFunctionName;
                    }

                    IList<Common.Tool> tools = parameters.Tools ??= [];
                    foreach (var tool in options.Tools)
                    {
                        switch (tool)
                        {
                            case AIFunctionDeclaration f:
                                // Only process tool schema when strict mode is enabled (requires additionalProperties: false)
                                var toolSchema = useStrictTools
                                    ? ProcessToolSchema(f.JsonSchema)
                                    : JsonSerializer.SerializeToNode(JsonSerializer.Deserialize<FunctionParameters>(f.JsonSchema));
                                tools.Add(new Common.Tool(new Function(f.Name, f.Description, toolSchema)
                                {
                                    Strict = useStrictTools ? true : null
                                }));
                                break;

                            case HostedCodeInterpreterTool:
                                tools.Add(Common.Tool.CodeInterpreter);
                                break;

                            case HostedWebSearchTool:
                                tools.Add(ServerTools.GetWebSearchTool(5));
                                break;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
                            case HostedMcpServerTool mcpt:
                                MCPServer mcpServer = new()
                                {
                                    Url = mcpt.ServerAddress,
                                    Name = mcpt.ServerName,
                                };

                                if (mcpt.AllowedTools is not null)
                                {
                                    mcpServer.ToolConfiguration.AllowedTools.AddRange(mcpt.AllowedTools);
                                }

                                mcpServer.AuthorizationToken = mcpt.AuthorizationToken;

                                (parameters.MCPServers ??= []).Add(mcpServer);
                                break;
#pragma warning restore MEAI001
                        }
                    }
                }

                // Map thinking parameters from ChatOptions
                var thinkingParameters = options.GetThinkingParameters();
                if (thinkingParameters != null)
                {
                    parameters.Thinking = thinkingParameters;
                }

                // Map response format from ChatOptions for structured JSON output
                if (options.ResponseFormat is ChatResponseFormatJson jsonFormat && jsonFormat.Schema is JsonElement schema)
                {
                    // Anthropic requires additionalProperties: false on all object types
                    var processedSchema = EnsureAdditionalPropertiesFalse(schema);

                    parameters.OutputFormat = new OutputFormat
                    {
                        Type = "json_schema",
                        Schema = processedSchema
                    };
                }
            }

            foreach (ChatMessage message in messages)
            {
                if (message.Role == ChatRole.System)
                {
                    (parameters.System ??= []).Add(new SystemMessage(string.Concat(message.Contents.OfType<Microsoft.Extensions.AI.TextContent>())));
                }
                else
                {
                    // Process contents in order, creating new messages when switching between tool results and other content
                    // This preserves ordering and handles interleaved tool calls, AI output, and tool results
                    Message currentMessage = null;
                    bool lastWasToolResult = false;
                    
                    foreach (AIContent content in message.Contents)
                    {
                        bool isToolResult = content is Microsoft.Extensions.AI.FunctionResultContent;
                        
                        // Create new message if:
                        // 1. This is the first content item, OR
                        // 2. We're switching between tool result and non-tool result content
                        if (currentMessage == null || lastWasToolResult != isToolResult)
                        {
                            currentMessage = new()
                            {
                                // Tool results must always be in User messages, others respect original role
                                Role = isToolResult ? RoleType.User : (message.Role == ChatRole.Assistant ? RoleType.Assistant : RoleType.User),
                                Content = [],
                            };
                            parameters.Messages.Add(currentMessage);
                            lastWasToolResult = isToolResult;
                        }
                        
                        // Add content to current message
                        switch (content)
                        {
                            case Microsoft.Extensions.AI.FunctionResultContent frc:
                                currentMessage.Content.Add(new ToolResultContent()
                                {
                                    ToolUseId = frc.CallId,
                                    Content = new List<ContentBase>() { new TextContent () { Text = frc.Result?.ToString() ?? string.Empty } },
                                    IsError = frc.Exception is not null,
                                });
                                break;

                            case Microsoft.Extensions.AI.TextReasoningContent reasoningContent:
                                if (string.IsNullOrEmpty(reasoningContent.Text))
                                {
                                    currentMessage.Content.Add(new Messaging.RedactedThinkingContent() { Data = reasoningContent.ProtectedData });
                                }
                                else
                                {
                                    currentMessage.Content.Add(new Messaging.ThinkingContent()
                                    {
                                        Thinking = reasoningContent.Text,
                                        Signature = reasoningContent.ProtectedData,
                                    });
                                }
                                break;

                            case Microsoft.Extensions.AI.TextContent textContent:
                                string text = textContent.Text;
                                if (currentMessage.Role == RoleType.Assistant)
                                {
                                    text.TrimEnd();
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        currentMessage.Content.Add(new TextContent() { Text = text });
                                    }
                                }
                                else if (!string.IsNullOrWhiteSpace(text))
                                {
                                    currentMessage.Content.Add(new TextContent() { Text = text });
                                }

                                break;

                            case Microsoft.Extensions.AI.DataContent imageContent when imageContent.HasTopLevelMediaType("image"):
                                currentMessage.Content.Add(new ImageContent()
                                {
                                    Source = new()
                                    {
                                        Data = Convert.ToBase64String(imageContent.Data.ToArray()),
                                        MediaType = imageContent.MediaType,
                                    }
                                });
                                break;

                            case Microsoft.Extensions.AI.DataContent documentContent when documentContent.HasTopLevelMediaType("application"):
                                currentMessage.Content.Add(new DocumentContent()
                                {
                                    Source = new()
                                    {
                                        Data = Convert.ToBase64String(documentContent.Data.ToArray()),
                                        MediaType = documentContent.MediaType,
                                    }
                                });
                                break;

                            case Microsoft.Extensions.AI.FunctionCallContent fcc:
                                currentMessage.Content.Add(new ToolUseContent()
                                {
                                    Id = fcc.CallId,
                                    Name = fcc.Name,
                                    Input = JsonSerializer.SerializeToNode(fcc.Arguments)
                                });
                                break;
                        }
                    }
                }
            }

            parameters.Messages.RemoveAll(m => m.Content.Count == 0);

            // Avoid errors from completely empty input.
            if (!parameters.Messages.Any(m => m.Content.Count > 0))
            {
                parameters.Messages.Add(new(RoleType.User, "\u200b")); // zero-width space
            }

            return parameters;
        }

        /// <summary>
        /// Process response content
        /// </summary>
        public static List<AIContent> ProcessResponseContent(MessageResponse response)
        {
            List<AIContent> contents = new();

            foreach (ContentBase content in response.Content)
            {
                switch (content)
                {
                    case Messaging.ThinkingContent thinkingContent:
                        contents.Add(new Microsoft.Extensions.AI.TextReasoningContent(thinkingContent.Thinking)
                        {
                            ProtectedData = thinkingContent.Signature,
                        });
                        break;

                    case Messaging.RedactedThinkingContent redactedThinkingContent:
                        contents.Add(new Microsoft.Extensions.AI.TextReasoningContent(null)
                        {
                            ProtectedData = redactedThinkingContent.Data,
                        });
                        break;

                    case TextContent tc:
                        var textContent = new Microsoft.Extensions.AI.TextContent(tc.Text);
                        if (tc.Citations != null && tc.Citations.Any())
                        {
                            foreach (var tau in tc.Citations)
                            {
                                (textContent.Annotations ?? []).Add(new CitationAnnotation
                                {
                                    RawRepresentation = tau,
                                    AnnotatedRegions =
                                    [
                                        new TextSpanAnnotatedRegion
                                            { StartIndex = (int?)tau.StartPageNumber, EndIndex = (int?)tau.EndPageNumber }
                                    ],
                                    FileId = tau.Title
                                });
                            }
                        }
                        contents.Add(textContent);
                        break;

                    case ImageContent ic:
                        contents.Add(new Microsoft.Extensions.AI.DataContent(ic.Source.Data, ic.Source.MediaType));
                        break;

                    case ToolUseContent tuc:
                        contents.Add(new FunctionCallContent(
                            tuc.Id,
                            tuc.Name,
                            tuc.Input is not null ? tuc.Input.Deserialize<Dictionary<string, object>>() : null));
                        break;

                    case ToolResultContent trc:
                        contents.Add(new FunctionResultContent(
                            trc.ToolUseId,
                            trc.Content));
                        break;
                }
            }

            return contents;
        }

        /// <summary>
        /// Processes a tool's JSON schema to ensure all object types have additionalProperties: false,
        /// as required by Anthropic's structured output API.
        /// </summary>
        private static JsonNode ProcessToolSchema(JsonElement schema)
        {
            var node = JsonNode.Parse(schema.GetRawText());
            if (node is JsonObject obj)
            {
                ProcessSchemaNode(obj);
            }
            return node;
        }

        /// <summary>
        /// Processes a JSON schema to ensure all object types have additionalProperties: false,
        /// as required by Anthropic's structured output API.
        /// </summary>
        private static JsonElement EnsureAdditionalPropertiesFalse(JsonElement schema)
        {
            var node = JsonNode.Parse(schema.GetRawText());
            if (node is JsonObject obj)
            {
                ProcessSchemaNode(obj);
            }
            return JsonDocument.Parse(node.ToJsonString()).RootElement;
        }

        private static void ProcessSchemaNode(JsonNode node)
        {
            if (node is not JsonObject obj)
                return;

            // If this is an object type, ensure additionalProperties is false
            if (obj.TryGetPropertyValue("type", out var typeNode) &&
                typeNode?.GetValue<string>() == "object")
            {
                obj["additionalProperties"] = false;
            }

            // Process nested properties
            if (obj.TryGetPropertyValue("properties", out var propsNode) && propsNode is JsonObject props)
            {
                foreach (var prop in props)
                {
                    if (prop.Value is JsonObject propObj)
                    {
                        ProcessSchemaNode(propObj);
                    }
                }
            }

            // Process array items
            if (obj.TryGetPropertyValue("items", out var itemsNode))
            {
                ProcessSchemaNode(itemsNode);
            }

            // Process $defs / definitions
            if (obj.TryGetPropertyValue("$defs", out var defsNode) && defsNode is JsonObject defs)
            {
                foreach (var def in defs)
                {
                    ProcessSchemaNode(def.Value);
                }
            }

            if (obj.TryGetPropertyValue("definitions", out var definitionsNode) && definitionsNode is JsonObject definitions)
            {
                foreach (var def in definitions)
                {
                    ProcessSchemaNode(def.Value);
                }
            }

            // Process anyOf/oneOf/allOf
            foreach (var keyword in new[] { "anyOf", "oneOf", "allOf" })
            {
                if (obj.TryGetPropertyValue(keyword, out var arrayNode) && arrayNode is JsonArray arr)
                {
                    foreach (var item in arr)
                    {
                        ProcessSchemaNode(item);
                    }
                }
            }
        }

        /// <summary>
        /// Function parameters class
        /// </summary>
        private sealed class FunctionParameters
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "object";

            [JsonPropertyName("required")]
            public List<string> Required { get; set; } = [];

            [JsonPropertyName("properties")]
            public Dictionary<string, JsonElement> Properties { get; set; } = [];
        }
    }
}