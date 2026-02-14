using Microsoft.Extensions.DependencyInjection;
using SkillsDotNet.Mcp;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Vendor-specific convenience methods for registering skills from well-known directories.
/// </summary>
public static class VendorSkillExtensions
{
    /// <summary>
    /// Register skills from <c>~/.claude/skills/</c>.
    /// </summary>
    public static IMcpServerBuilder WithClaudeSkills(this IMcpServerBuilder builder, SkillOptions? options = null)
        => builder.WithSkillsDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "skills"),
            options);

    /// <summary>
    /// Register skills from <c>~/.cursor/skills/</c>.
    /// </summary>
    public static IMcpServerBuilder WithCursorSkills(this IMcpServerBuilder builder, SkillOptions? options = null)
        => builder.WithSkillsDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cursor", "skills"),
            options);

    /// <summary>
    /// Register skills from <c>~/.copilot/skills/</c>.
    /// </summary>
    public static IMcpServerBuilder WithCopilotSkills(this IMcpServerBuilder builder, SkillOptions? options = null)
        => builder.WithSkillsDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".copilot", "skills"),
            options);

    /// <summary>
    /// Register skills from <c>/etc/codex/skills/</c> (system) and <c>~/.codex/skills/</c> (user).
    /// System skills take priority (first-wins deduplication).
    /// </summary>
    public static IMcpServerBuilder WithCodexSkills(this IMcpServerBuilder builder, SkillOptions? options = null)
        => builder.WithSkillsDirectory(
            new[]
            {
                "/etc/codex/skills",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "skills")
            },
            options);

    /// <summary>
    /// Register skills from <c>~/.gemini/skills/</c>.
    /// </summary>
    public static IMcpServerBuilder WithGeminiSkills(this IMcpServerBuilder builder, SkillOptions? options = null)
        => builder.WithSkillsDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "skills"),
            options);

    /// <summary>
    /// Register skills from <c>~/.config/agents/skills/</c>.
    /// </summary>
    public static IMcpServerBuilder WithGooseSkills(this IMcpServerBuilder builder, SkillOptions? options = null)
        => builder.WithSkillsDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "agents", "skills"),
            options);

    /// <summary>
    /// Register skills from <c>~/.config/opencode/skills/</c>.
    /// </summary>
    public static IMcpServerBuilder WithOpenCodeSkills(this IMcpServerBuilder builder, SkillOptions? options = null)
        => builder.WithSkillsDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "opencode", "skills"),
            options);
}
