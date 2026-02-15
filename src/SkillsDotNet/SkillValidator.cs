using System.Globalization;
using System.Text;

namespace SkillsDotNet;

/// <summary>
/// Validates skill properties according to the agentskills.io specification.
/// </summary>
public static class SkillValidator
{
    /// <summary>Maximum length of a skill name.</summary>
    public const int MaxSkillNameLength = 64;

    /// <summary>Maximum length of a skill description.</summary>
    public const int MaxDescriptionLength = 1024;

    /// <summary>Maximum length of the compatibility field.</summary>
    public const int MaxCompatibilityLength = 500;

    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "name", "description", "license", "allowed-tools", "metadata", "compatibility"
    };

    /// <summary>
    /// Validates a skill's frontmatter metadata and directory name.
    /// Returns a list of error messages. An empty list means the skill is valid.
    /// </summary>
    /// <param name="frontmatter">Parsed frontmatter key-value pairs.</param>
    /// <param name="directoryName">The name of the skill's directory, or null to skip directory name matching.</param>
    public static IReadOnlyList<string> Validate(
        IReadOnlyDictionary<string, object> frontmatter,
        string? directoryName = null)
    {
        ArgumentNullException.ThrowIfNull(frontmatter);

        var errors = new List<string>();

        // Check for disallowed fields
        foreach (var key in frontmatter.Keys)
        {
            if (!AllowedFields.Contains(key))
            {
                errors.Add($"Unknown field '{key}'. Allowed fields: {string.Join(", ", AllowedFields)}.");
            }
        }

        // Validate name
        if (frontmatter.TryGetValue("name", out var nameObj) && nameObj is string name)
        {
            ValidateName(name, directoryName, errors);
        }
        else
        {
            errors.Add("Field 'name' is required and must be a string.");
        }

        // Validate description
        if (frontmatter.TryGetValue("description", out var descObj) && descObj is string description)
        {
            ValidateDescription(description, errors);
        }
        else
        {
            errors.Add("Field 'description' is required and must be a string.");
        }

        // Validate compatibility (optional)
        if (frontmatter.TryGetValue("compatibility", out var compatObj))
        {
            if (compatObj is string compat)
            {
                if (compat.Length > MaxCompatibilityLength)
                {
                    errors.Add($"Field 'compatibility' must be at most {MaxCompatibilityLength} characters.");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Validates a skill name according to the agentskills.io specification.
    /// </summary>
    public static IReadOnlyList<string> ValidateName(string name, string? directoryName = null)
    {
        var errors = new List<string>();
        ValidateName(name, directoryName, errors);
        return errors;
    }

    private static void ValidateName(string name, string? directoryName, List<string> errors)
    {
        if (string.IsNullOrEmpty(name))
        {
            errors.Add("Skill name must not be empty.");
            return;
        }

        // NFKC normalization
        var normalized = name.Normalize(NormalizationForm.FormKC);

        if (normalized.Length > MaxSkillNameLength)
        {
            errors.Add($"Skill name must be at most {MaxSkillNameLength} characters (after NFKC normalization).");
        }

        if (normalized != normalized.ToLowerInvariant())
        {
            errors.Add("Skill name must be all lowercase.");
        }

        if (normalized.StartsWith('-') || normalized.EndsWith('-'))
        {
            errors.Add("Skill name must not start or end with a hyphen.");
        }

        if (normalized.Contains("--"))
        {
            errors.Add("Skill name must not contain consecutive hyphens.");
        }

        foreach (var c in normalized)
        {
            if (!char.IsLetterOrDigit(c) && c != '-')
            {
                errors.Add($"Skill name contains invalid character '{c}'. Only letters, digits, and hyphens are allowed.");
                break;
            }
        }

        // Directory name must match
        if (directoryName is not null)
        {
            var normalizedDirName = directoryName.Normalize(NormalizationForm.FormKC);
            if (!string.Equals(normalized, normalizedDirName, StringComparison.Ordinal))
            {
                errors.Add($"Skill name '{normalized}' does not match directory name '{normalizedDirName}'.");
            }
        }
    }

    private static void ValidateDescription(string description, List<string> errors)
    {
        if (string.IsNullOrEmpty(description))
        {
            errors.Add("Skill description must not be empty.");
            return;
        }

        if (description.Length > MaxDescriptionLength)
        {
            errors.Add($"Skill description must be at most {MaxDescriptionLength} characters.");
        }
    }
}
