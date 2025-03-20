using System;

namespace Anthropic.SDK.Messaging;

/// <summary>
/// Prompt Cache Type Definitions. 
/// </summary>
[Flags]
public enum PromptCacheType
{
    /// <summary>
    /// No Prompt Caching
    /// </summary>
    None = 0,
    /// <summary>
    /// Use the cache-control instructions from each message for fine-grained control
    /// </summary>
    FineGrained = 1,
    /// <summary>
    /// Use the cache-control instructions from the system messages for automatic tools and system message caching
    /// </summary>
    AutomaticToolsAndSystem = 2,
}