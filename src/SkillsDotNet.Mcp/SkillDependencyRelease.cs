namespace SkillsDotNet.Mcp;

/// <summary>
/// Describes the MCP server dependencies that are no longer needed after a skill is unloaded.
/// Passed to the <see cref="SkillCatalog.OnDependenciesReleased"/> callback.
/// Only servers that are not still required by any other currently-loaded skill are included,
/// so the host can disconnect them without ref-counting.
/// </summary>
public sealed class SkillDependencyRelease
{
    /// <summary>The name of the skill that was unloaded.</summary>
    public required string SkillName { get; init; }

    /// <summary>
    /// MCP server names that were declared as dependencies of the unloaded skill and
    /// are no longer required by any other currently-loaded skill. Safe to disconnect.
    /// </summary>
    public required IReadOnlyList<string> ServerNames { get; init; }
}
