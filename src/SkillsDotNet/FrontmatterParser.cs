using System.Text.RegularExpressions;

namespace SkillsDotNet;

/// <summary>
/// Simple YAML frontmatter parser (no external YAML dependency).
/// Parses the <c>---</c>-delimited frontmatter block from a Markdown file.
/// </summary>
public static class FrontmatterParser
{
    private static readonly Regex ClosingFencePattern = new(@"\n---\s*\n", RegexOptions.Compiled);

    /// <summary>
    /// Parses frontmatter from the given content string.
    /// Returns a tuple of (frontmatter dictionary, body text after frontmatter).
    /// If no valid frontmatter is found, returns an empty dictionary and the original content.
    /// </summary>
    public static (IReadOnlyDictionary<string, object> Frontmatter, string Body) Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.StartsWith("---"))
        {
            return (new Dictionary<string, object>(), content);
        }

        // Find the closing --- fence
        var match = ClosingFencePattern.Match(content, 3);
        if (!match.Success)
        {
            return (new Dictionary<string, object>(), content);
        }

        var yamlBlock = content.Substring(3, match.Index - 3).Trim();
        var body = content.Substring(match.Index + match.Length);

        var frontmatter = ParseYamlBlock(yamlBlock);
        return (frontmatter, body);
    }

    private static Dictionary<string, object> ParseYamlBlock(string yaml)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        string? currentKey = null;
        Dictionary<string, string>? nestedDict = null;

        foreach (var rawLine in yaml.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            // Check if this is an indented line (nested under a key like metadata:)
            if (currentKey is not null && nestedDict is not null && (line.StartsWith("  ") || line.StartsWith("\t")))
            {
                var trimmed = line.TrimStart();
                var nestedColonIndex = trimmed.IndexOf(':');
                if (nestedColonIndex > 0)
                {
                    var nestedKey = trimmed.Substring(0, nestedColonIndex).Trim();
                    var nestedValue = trimmed.Substring(nestedColonIndex + 1).Trim();
                    nestedDict[nestedKey] = StripQuotes(nestedValue);
                }
                continue;
            }

            // Flush any pending nested dict
            if (currentKey is not null && nestedDict is not null)
            {
                result[currentKey] = nestedDict;
                currentKey = null;
                nestedDict = null;
            }

            // Parse top-level key: value
            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var key = line.Substring(0, colonIndex).Trim();
            var rawValue = line.Substring(colonIndex + 1).Trim();

            if (string.IsNullOrEmpty(rawValue))
            {
                // Could be the start of a nested block (e.g. "metadata:")
                currentKey = key;
                nestedDict = new Dictionary<string, string>(StringComparer.Ordinal);
            }
            else if (rawValue.StartsWith('[') && rawValue.EndsWith(']'))
            {
                // Inline list: [a, b, c]
                var inner = rawValue.Substring(1, rawValue.Length - 2);
                var items = inner.Split(',')
                    .Select(s => StripQuotes(s.Trim()))
                    .Where(s => s.Length > 0)
                    .ToList();
                result[key] = items;
            }
            else
            {
                result[key] = StripQuotes(rawValue);
            }
        }

        // Flush any remaining nested dict
        if (currentKey is not null && nestedDict is not null)
        {
            result[currentKey] = nestedDict;
        }

        return result;
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if ((value[0] == '"' && value[^1] == '"') ||
                (value[0] == '\'' && value[^1] == '\''))
            {
                return value.Substring(1, value.Length - 2);
            }
        }

        return value;
    }
}
