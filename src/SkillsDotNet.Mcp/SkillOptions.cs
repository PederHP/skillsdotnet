namespace SkillsDotNet.Mcp;

/// <summary>
/// Configuration options for skill registration.
/// </summary>
public sealed class SkillOptions
{
    /// <summary>
    /// How to expose supporting files: <see cref="SkillFileMode.Template"/> (default) uses a
    /// resource template so files are not listed individually, or <see cref="SkillFileMode.Resources"/>
    /// lists every file as a separate resource.
    /// </summary>
    public SkillFileMode SupportingFiles { get; set; } = SkillFileMode.Template;

    /// <summary>
    /// Main file name to look for in each skill directory. Default is "SKILL.md".
    /// </summary>
    public string MainFileName { get; set; } = "SKILL.md";
}

/// <summary>
/// Controls how supporting files within a skill are exposed as MCP resources.
/// </summary>
public enum SkillFileMode
{
    /// <summary>
    /// Supporting files are accessible via a resource template and not listed individually.
    /// </summary>
    Template,

    /// <summary>
    /// All files are listed as individual resources.
    /// </summary>
    Resources
}
