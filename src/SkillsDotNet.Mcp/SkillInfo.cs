namespace SkillsDotNet.Mcp;

/// <summary>
/// Parsed skill metadata produced by scanning a skill directory.
/// </summary>
public sealed class SkillInfo
{
    /// <summary>
    /// The skill name (matches the directory name).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of the skill.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Absolute path to the skill directory.
    /// </summary>
    public required string SkillDirectoryPath { get; init; }

    /// <summary>
    /// Name of the main skill file (e.g. "SKILL.md").
    /// </summary>
    public required string MainFileName { get; init; }

    /// <summary>
    /// All files within the skill directory.
    /// </summary>
    public IReadOnlyList<SkillFileInfo> Files { get; init; } = [];

    /// <summary>
    /// Parsed frontmatter key-value pairs from the main skill file.
    /// </summary>
    public IReadOnlyDictionary<string, object> Frontmatter { get; init; } =
        new Dictionary<string, object>();
}
