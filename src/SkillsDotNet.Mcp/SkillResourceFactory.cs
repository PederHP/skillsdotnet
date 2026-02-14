using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace SkillsDotNet.Mcp;

/// <summary>
/// Creates <see cref="McpServerResource"/> instances from a <see cref="SkillInfo"/>.
/// </summary>
public static class SkillResourceFactory
{
    /// <summary>
    /// Creates MCP server resources for a given skill.
    /// </summary>
    /// <param name="skill">The parsed skill information.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>A list of <see cref="McpServerResource"/> instances to register.</returns>
    public static IReadOnlyList<McpServerResource> CreateResources(SkillInfo skill, SkillOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(skill);

        options ??= new SkillOptions();
        var resources = new List<McpServerResource>();

        // 1. Main SKILL.md resource (non-templated, listed)
        var mainFilePath = Path.Combine(skill.SkillDirectoryPath, skill.MainFileName);
        resources.Add(McpServerResource.Create(
            () => File.ReadAllTextAsync(mainFilePath),
            new McpServerResourceCreateOptions
            {
                UriTemplate = $"skill://{skill.Name}/{skill.MainFileName}",
                Name = skill.Name,
                Description = skill.Description,
                MimeType = "text/markdown"
            }));

        // 2. Manifest resource (non-templated, listed)
        var manifestJson = BuildManifestJson(skill);
        resources.Add(McpServerResource.Create(
            () => manifestJson,
            new McpServerResourceCreateOptions
            {
                UriTemplate = $"skill://{skill.Name}/_manifest",
                Name = $"{skill.Name}-manifest",
                Description = $"File manifest for skill '{skill.Name}'",
                MimeType = "application/json"
            }));

        // 3. Supporting files
        if (options.SupportingFiles == SkillFileMode.Template)
        {
            // Single resource template for all supporting files
            resources.Add(McpServerResource.Create(
                (string path) => ReadSupportingFile(skill.SkillDirectoryPath, path),
                new McpServerResourceCreateOptions
                {
                    UriTemplate = $"skill://{skill.Name}/{{+path}}",
                    Name = $"{skill.Name}-files",
                    Description = $"Supporting files for skill '{skill.Name}'"
                }));
        }
        else
        {
            // Each supporting file as an individual resource
            foreach (var file in skill.Files)
            {
                if (file.Path == skill.MainFileName)
                {
                    continue; // Already registered above
                }

                var filePath = Path.Combine(skill.SkillDirectoryPath, file.Path.Replace('/', Path.DirectorySeparatorChar));
                var mimeType = DetectMimeType(file.Path);
                resources.Add(McpServerResource.Create(
                    () => ReadFileContents(filePath, mimeType),
                    new McpServerResourceCreateOptions
                    {
                        UriTemplate = $"skill://{skill.Name}/{file.Path}",
                        Name = $"{skill.Name}/{file.Path}",
                        Description = $"File '{file.Path}' in skill '{skill.Name}'",
                        MimeType = mimeType
                    }));
            }
        }

        return resources;
    }

    private static string BuildManifestJson(SkillInfo skill)
    {
        var manifest = new
        {
            skill = skill.Name,
            files = skill.Files.Select(f => new
            {
                path = f.Path,
                size = f.Size,
                hash = f.Hash
            })
        };

        return JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    internal static ResourceContents ReadSupportingFile(string skillDir, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        // Security: prevent path traversal
        if (Path.IsPathRooted(path) || path.Contains(".."))
        {
            throw new ArgumentException("Invalid path: absolute paths and path traversal are not allowed.", nameof(path));
        }

        var nativePath = path.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(skillDir, nativePath));
        var normalizedSkillDir = Path.GetFullPath(skillDir);

        if (!fullPath.StartsWith(normalizedSkillDir, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid path: path traversal detected.", nameof(path));
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {path}");
        }

        var mimeType = DetectMimeType(path);
        return ReadFileContentsSync(fullPath, mimeType, $"skill://resource/{path}");
    }

    private static ResourceContents ReadFileContentsSync(string filePath, string mimeType, string uri)
    {
        if (IsTextMimeType(mimeType))
        {
            return new TextResourceContents
            {
                Uri = uri,
                MimeType = mimeType,
                Text = File.ReadAllText(filePath)
            };
        }

        var bytes = File.ReadAllBytes(filePath);
        return new BlobResourceContents
        {
            Uri = uri,
            MimeType = mimeType,
            Blob = Convert.ToBase64String(bytes)
        };
    }

    private static async Task<ResourceContents> ReadFileContents(string filePath, string mimeType)
    {
        if (IsTextMimeType(mimeType))
        {
            var text = await File.ReadAllTextAsync(filePath);
            return new TextResourceContents
            {
                Uri = $"skill://resource/{Path.GetFileName(filePath)}",
                MimeType = mimeType,
                Text = text
            };
        }

        var bytes = await File.ReadAllBytesAsync(filePath);
        return new BlobResourceContents
        {
            Uri = $"skill://resource/{Path.GetFileName(filePath)}",
            MimeType = mimeType,
            Blob = Convert.ToBase64String(bytes)
        };
    }

    private static bool IsTextMimeType(string mimeType)
    {
        return mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               mimeType == "application/json" ||
               mimeType == "application/xml" ||
               mimeType == "application/javascript" ||
               mimeType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) ||
               mimeType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
    }

    internal static string DetectMimeType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".yaml" or ".yml" => "text/yaml",
            ".xml" => "application/xml",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".ts" => "text/typescript",
            ".py" => "text/x-python",
            ".cs" => "text/x-csharp",
            ".java" => "text/x-java",
            ".rb" => "text/x-ruby",
            ".go" => "text/x-go",
            ".rs" => "text/x-rust",
            ".sh" or ".bash" => "text/x-shellscript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
