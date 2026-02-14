namespace SkillsDotNet.Mcp;

/// <summary>
/// Client-side discovery model representing a skill found on an MCP server.
/// </summary>
public sealed class SkillSummary
{
    /// <summary>
    /// The skill name extracted from the resource URI.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description from the resource metadata.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The full URI of the skill's main resource (e.g. "skill://my-skill/SKILL.md").
    /// </summary>
    public required string Uri { get; init; }
}
