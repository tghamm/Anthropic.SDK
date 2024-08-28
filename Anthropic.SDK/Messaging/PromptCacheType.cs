using System;

namespace Anthropic.SDK.Messaging;

/// <summary>
/// Prompt Cache Type Definitions. Designed to be used as a bitwise assignment if you want to cache multiple types and are caching enough context.
/// </summary>
[Flags]
public enum PromptCacheType
{
    /// <summary>
    /// No Prompt Caching
    /// </summary>
    None = 0,
    /// <summary>
    /// Cache System and User Messages
    /// </summary>
    Messages = 1 << 0, // 1
    /// <summary>
    /// Cache Tool Definitions
    /// </summary>
    Tools = 1 << 1, // 2
    /// <summary>
    /// Use the cache-control instructions from each message
    /// </summary>
    FineGrained = 1 << 2, // 4
}