namespace SkillsDotNet;

/// <summary>
/// Extension methods for extracting typed fields from skill frontmatter dictionaries.
/// </summary>
public static class SkillFrontmatterExtensions
{
    /// <summary>
    /// Returns the list of MCP server dependency names declared in the frontmatter,
    /// or an empty list if the <c>dependencies</c> field is absent or not a list.
    /// </summary>
    public static IReadOnlyList<string> GetDependencies(
        this IReadOnlyDictionary<string, object> frontmatter)
    {
        ArgumentNullException.ThrowIfNull(frontmatter);

        if (frontmatter.TryGetValue("dependencies", out var value) && value is List<string> list)
        {
            return list;
        }

        return [];
    }

    /// <summary>
    /// Returns the list of MCP server dependency names declared in the skill's frontmatter,
    /// or an empty list if the <c>dependencies</c> field is absent or not a list.
    /// </summary>
    public static IReadOnlyList<string> GetDependencies(this SkillInfo skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return skill.Frontmatter.GetDependencies();
    }
}
