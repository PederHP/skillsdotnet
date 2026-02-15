using Microsoft.Extensions.AI;

namespace SkillsDotNet;

/// <summary>
/// Helpers for converting skill frontmatter into <see cref="TextContent"/> context blocks.
/// </summary>
public static class SkillContextExtensions
{
    /// <summary>
    /// Default formatter that produces "<c>name: description</c>" from frontmatter.
    /// </summary>
    public static readonly Func<IReadOnlyDictionary<string, object>, string> DefaultFormatter =
        frontmatter =>
        {
            var name = frontmatter.TryGetValue("name", out var n) ? n?.ToString() ?? "" : "";
            var description = frontmatter.TryGetValue("description", out var d) ? d?.ToString() ?? "" : "";
            return $"{name}: {description}";
        };

    /// <summary>
    /// Converts this <see cref="SkillInfo"/>'s frontmatter into a <see cref="TextContent"/> context block.
    /// </summary>
    public static TextContent AsTextContent(
        this SkillInfo skill,
        Func<IReadOnlyDictionary<string, object>, string>? formatter = null)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return ToTextContent(skill.Frontmatter, formatter);
    }

    /// <summary>
    /// Converts a raw frontmatter dictionary into a <see cref="TextContent"/> context block.
    /// </summary>
    public static TextContent ToTextContent(
        IReadOnlyDictionary<string, object> frontmatter,
        Func<IReadOnlyDictionary<string, object>, string>? formatter = null)
    {
        ArgumentNullException.ThrowIfNull(frontmatter);
        var text = (formatter ?? DefaultFormatter)(frontmatter);
        return new TextContent(text);
    }
}
