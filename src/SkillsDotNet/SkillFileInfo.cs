namespace SkillsDotNet;

/// <summary>
/// Represents a single file within a skill directory.
/// </summary>
public sealed class SkillFileInfo
{
    /// <summary>
    /// POSIX-style relative path within the skill directory.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Content hash in "sha256:&lt;hex&gt;" format.
    /// </summary>
    public required string Hash { get; init; }
}
