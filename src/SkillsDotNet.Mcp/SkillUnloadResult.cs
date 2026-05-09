namespace SkillsDotNet.Mcp;

/// <summary>
/// Result of <see cref="SkillCatalog.UnloadSkillAsync"/>. Hosts use this to scrub the
/// chat history (drop the tool results identified by <see cref="ToolCallIds"/>) and to
/// learn which MCP servers, if any, were released.
/// </summary>
public sealed class SkillUnloadResult
{
    /// <summary>The name of the skill that was unloaded.</summary>
    public required string SkillName { get; init; }

    /// <summary>
    /// Tool-call IDs of every <c>load_skill</c> invocation that previously loaded this skill,
    /// captured from the active <c>FunctionInvokingChatClient</c> context at the moment of load.
    /// Empty if the host did not use a function-invoking chat client (no IDs were observable).
    /// </summary>
    public required IReadOnlyList<string> ToolCallIds { get; init; }

    /// <summary>
    /// MCP server names that were dependencies of the unloaded skill and are no longer
    /// required by any other currently-loaded skill. Safe to disconnect.
    /// Mirrors what was passed to <see cref="SkillCatalog.OnDependenciesReleased"/>.
    /// </summary>
    public required IReadOnlyList<string> ReleasedServers { get; init; }
}
