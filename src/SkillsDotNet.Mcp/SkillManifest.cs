using System.Text.Json.Serialization;

namespace SkillsDotNet.Mcp;

/// <summary>
/// Client-side manifest model deserialized from a skill's _manifest resource.
/// </summary>
public sealed class SkillManifest
{
    /// <summary>
    /// The skill name.
    /// </summary>
    [JsonPropertyName("skill")]
    public required string Skill { get; init; }

    /// <summary>
    /// List of files contained in the skill.
    /// </summary>
    [JsonPropertyName("files")]
    public required IReadOnlyList<SkillManifestFile> Files { get; init; }
}

/// <summary>
/// A single file entry in a skill manifest.
/// </summary>
public sealed class SkillManifestFile
{
    /// <summary>
    /// POSIX-style relative path within the skill directory.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public required long Size { get; init; }

    /// <summary>
    /// Content hash in "sha256:&lt;hex&gt;" format.
    /// </summary>
    [JsonPropertyName("hash")]
    public required string Hash { get; init; }
}
