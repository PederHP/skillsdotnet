namespace SkillsDotNet.Mcp;

/// <summary>
/// Result of <see cref="SkillCatalog.UnloadSkillAsync"/>, also passed to
/// <see cref="SkillCatalog.OnSkillUnloaded"/>. Hosts use it to decide which MCP
/// servers to disconnect and which <c>load_skill</c> call/result pair to scrub
/// from their chat history.
/// </summary>
public sealed class SkillUnloadResult
{
    /// <summary>The name of the skill that was unloaded.</summary>
    public required string SkillName { get; init; }

    /// <summary>
    /// MCP server names that were declared as dependencies of the unloaded skill and
    /// are no longer required by any other currently-loaded skill. Safe to disconnect
    /// without ref-counting. Possibly empty.
    /// </summary>
    public required IReadOnlyList<string> ReleasedServers { get; init; }
}
