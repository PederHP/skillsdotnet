using SkillsDotNet;

namespace SkillsDotNet.Mcp;

/// <summary>
/// Maps skill names to their supporting file paths for autocompletion of resource template parameters.
/// </summary>
internal sealed class SkillCompletionRegistry
{
    private readonly Dictionary<string, List<string>> _skillFiles = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers the supporting file paths for a skill (excludes the main file).
    /// </summary>
    /// <param name="skill">The parsed skill information.</param>
    public void RegisterSkill(SkillInfo skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        var paths = new List<string>(skill.Files.Count);
        foreach (var file in skill.Files)
        {
            paths.Add(file.Path);
        }

        _skillFiles[skill.Name] = paths;
    }

    /// <summary>
    /// Returns file paths for the given skill that start with the specified prefix, capped at 100 results.
    /// </summary>
    /// <param name="skillName">The skill name to look up.</param>
    /// <param name="prefix">The prefix to filter by.</param>
    /// <returns>Matching values, total count, and whether more results exist beyond the returned set.</returns>
    public (IList<string> Values, int Total, bool HasMore) GetCompletions(string skillName, string prefix)
    {
        if (!_skillFiles.TryGetValue(skillName, out var files))
        {
            return ([], 0, false);
        }

        var matched = new List<string>();
        foreach (var file in files)
        {
            if (file.StartsWith(prefix, StringComparison.Ordinal))
            {
                matched.Add(file);
            }
        }

        const int maxResults = 100;
        if (matched.Count <= maxResults)
        {
            return (matched, matched.Count, false);
        }

        return (matched.GetRange(0, maxResults), matched.Count, true);
    }
}
