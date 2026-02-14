using System.Security.Cryptography;

namespace SkillsDotNet.Mcp;

/// <summary>
/// Scans directories for skills, parsing SKILL.md files and computing file hashes.
/// </summary>
public static class SkillDirectoryScanner
{
    /// <summary>
    /// Scans a single skill directory and returns a <see cref="SkillInfo"/>.
    /// </summary>
    /// <param name="skillDirectory">Path to the skill directory (must contain a SKILL.md file).</param>
    /// <param name="mainFileName">The main file name to look for. Defaults to "SKILL.md".</param>
    /// <returns>Parsed skill information.</returns>
    /// <exception cref="FileNotFoundException">The main skill file was not found.</exception>
    public static SkillInfo ScanSkill(string skillDirectory, string mainFileName = "SKILL.md")
    {
        ArgumentNullException.ThrowIfNull(skillDirectory);

        var dirInfo = new DirectoryInfo(skillDirectory);
        if (!dirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Skill directory not found: {skillDirectory}");
        }

        var mainFilePath = FindMainFile(dirInfo, mainFileName);
        if (mainFilePath is null)
        {
            throw new FileNotFoundException(
                $"Main skill file '{mainFileName}' not found in '{skillDirectory}'.");
        }

        var content = File.ReadAllText(mainFilePath);
        var (frontmatter, body) = FrontmatterParser.Parse(content);

        var name = GetSkillName(frontmatter, dirInfo.Name);
        var description = GetDescription(frontmatter, body);
        var files = ScanFiles(dirInfo);

        return new SkillInfo
        {
            Name = name,
            Description = description,
            SkillDirectoryPath = dirInfo.FullName,
            MainFileName = Path.GetFileName(mainFilePath),
            Files = files,
            Frontmatter = frontmatter
        };
    }

    /// <summary>
    /// Scans a parent directory for skill subdirectories, returning a <see cref="SkillInfo"/>
    /// for each subdirectory that contains a main skill file.
    /// </summary>
    /// <param name="parentDirectory">Path to the directory containing skill subdirectories.</param>
    /// <param name="mainFileName">The main file name to look for. Defaults to "SKILL.md".</param>
    /// <returns>List of parsed skill information. Subdirectories that fail to parse are skipped.</returns>
    public static IReadOnlyList<SkillInfo> ScanDirectory(string parentDirectory, string mainFileName = "SKILL.md")
    {
        ArgumentNullException.ThrowIfNull(parentDirectory);

        if (!Directory.Exists(parentDirectory))
        {
            return [];
        }

        var skills = new List<SkillInfo>();
        foreach (var subDir in Directory.GetDirectories(parentDirectory).Order())
        {
            var dirInfo = new DirectoryInfo(subDir);
            var mainFile = FindMainFile(dirInfo, mainFileName);
            if (mainFile is null)
            {
                continue;
            }

            try
            {
                skills.Add(ScanSkill(subDir, mainFileName));
            }
            catch
            {
                // Silently skip directories that fail to parse (matching FastMCP behavior)
            }
        }

        return skills;
    }

    /// <summary>
    /// Finds the main skill file in a directory.
    /// Prefers exact case match (SKILL.md), falls back to case-insensitive match (skill.md).
    /// </summary>
    internal static string? FindMainFile(DirectoryInfo directory, string mainFileName)
    {
        // Prefer exact case match
        var exactPath = Path.Combine(directory.FullName, mainFileName);
        if (File.Exists(exactPath))
        {
            return exactPath;
        }

        // Fallback: case-insensitive search
        var files = directory.GetFiles(mainFileName, SearchOption.TopDirectoryOnly);
        if (files.Length > 0)
        {
            return files[0].FullName;
        }

        // Try lowercase variant
        var lowerPath = Path.Combine(directory.FullName, mainFileName.ToLowerInvariant());
        if (File.Exists(lowerPath))
        {
            return lowerPath;
        }

        return null;
    }

    private static string GetSkillName(IReadOnlyDictionary<string, object> frontmatter, string directoryName)
    {
        if (frontmatter.TryGetValue("name", out var nameObj) && nameObj is string name && !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return directoryName;
    }

    private static string GetDescription(IReadOnlyDictionary<string, object> frontmatter, string body)
    {
        // 1. From frontmatter description field
        if (frontmatter.TryGetValue("description", out var descObj) && descObj is string desc && !string.IsNullOrWhiteSpace(desc))
        {
            return desc;
        }

        // 2. From first heading in body
        foreach (var line in body.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#'))
            {
                var heading = trimmed.TrimStart('#').Trim();
                if (heading.Length > 0)
                {
                    return Truncate(heading, 200);
                }
            }
        }

        // 3. From first non-empty line in body
        foreach (var line in body.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                return Truncate(trimmed, 200);
            }
        }

        // 4. Default
        return "A skill";
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    private static IReadOnlyList<SkillFileInfo> ScanFiles(DirectoryInfo directory)
    {
        var files = new List<SkillFileInfo>();

        foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories).OrderBy(f => f.FullName, StringComparer.Ordinal))
        {
            var relativePath = Path.GetRelativePath(directory.FullName, file.FullName)
                .Replace('\\', '/'); // POSIX paths for cross-platform consistency

            files.Add(new SkillFileInfo
            {
                Path = relativePath,
                Size = file.Length,
                Hash = ComputeFileHash(file.FullName)
            });
        }

        return files;
    }

    internal static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return "sha256:" + Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
