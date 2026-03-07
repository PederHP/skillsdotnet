namespace SkillsDotNet.Mcp;

/// <summary>
/// Describes the MCP server dependencies required by a skill being loaded.
/// Passed to the <see cref="SkillCatalog.OnDependenciesRequired"/> callback.
/// </summary>
public sealed class SkillDependencyRequest
{
    /// <summary>The name of the skill being loaded.</summary>
    public required string SkillName { get; init; }

    /// <summary>The MCP server names declared in the skill's <c>dependencies</c> frontmatter field.</summary>
    public required IReadOnlyList<string> ServerNames { get; init; }
}
