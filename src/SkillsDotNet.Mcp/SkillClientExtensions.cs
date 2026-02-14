using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace SkillsDotNet.Mcp;

/// <summary>
/// Extension methods on <see cref="McpClient"/> for discovering and downloading skills.
/// </summary>
public static class SkillClientExtensions
{
    private const string SkillUriPrefix = "skill://";
    private const string SkillMdSuffix = "/SKILL.md";

    /// <summary>
    /// Discover skills by matching <c>skill://*/SKILL.md</c> in listed resources.
    /// </summary>
    public static async Task<IReadOnlyList<SkillSummary>> ListSkillsAsync(
        this McpClient client, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var resources = await client.ListResourcesAsync(cancellationToken: cancellationToken);
        var skills = new List<SkillSummary>();

        foreach (var resource in resources)
        {
            var uri = resource.Uri;
            if (uri.StartsWith(SkillUriPrefix, StringComparison.Ordinal) &&
                uri.EndsWith(SkillMdSuffix, StringComparison.Ordinal))
            {
                var name = uri.Substring(SkillUriPrefix.Length, uri.Length - SkillUriPrefix.Length - SkillMdSuffix.Length);
                skills.Add(new SkillSummary
                {
                    Name = name,
                    Description = resource.Description ?? "",
                    Uri = uri
                });
            }
        }

        return skills;
    }

    /// <summary>
    /// Read and parse the manifest for a specific skill.
    /// </summary>
    public static async Task<SkillManifest> GetSkillManifestAsync(
        this McpClient client, string skillName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(skillName);

        var manifestUri = $"skill://{skillName}/_manifest";
        var result = await client.ReadResourceAsync(manifestUri, cancellationToken: cancellationToken);

        var content = result.Contents.FirstOrDefault()
            ?? throw new InvalidOperationException($"No content returned for manifest of skill '{skillName}'.");

        string json;
        if (content is TextResourceContents textContent)
        {
            json = textContent.Text;
        }
        else
        {
            throw new InvalidOperationException($"Unexpected content type for manifest of skill '{skillName}'.");
        }

        return JsonSerializer.Deserialize<SkillManifest>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize manifest for skill '{skillName}'.");
    }

    /// <summary>
    /// Download a skill (all files) to a local directory.
    /// Returns the path to the downloaded skill directory.
    /// </summary>
    public static async Task<string> DownloadSkillAsync(
        this McpClient client, string skillName, string targetDirectory,
        bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(skillName);
        ArgumentNullException.ThrowIfNull(targetDirectory);

        var manifest = await client.GetSkillManifestAsync(skillName, cancellationToken);
        var skillDir = Path.Combine(targetDirectory, skillName);

        if (Directory.Exists(skillDir) && !overwrite)
        {
            throw new IOException($"Skill directory already exists: {skillDir}. Set overwrite=true to replace.");
        }

        Directory.CreateDirectory(skillDir);

        foreach (var file in manifest.Files)
        {
            // Security: reject absolute paths and path traversal
            if (Path.IsPathRooted(file.Path) || file.Path.Contains(".."))
            {
                throw new InvalidOperationException($"Invalid file path in manifest: {file.Path}");
            }

            var fileUri = $"skill://{skillName}/{file.Path}";
            var result = await client.ReadResourceAsync(fileUri, cancellationToken: cancellationToken);

            var content = result.Contents.FirstOrDefault();
            if (content is null)
            {
                continue;
            }

            var localPath = Path.Combine(skillDir, file.Path.Replace('/', Path.DirectorySeparatorChar));
            var localDir = Path.GetDirectoryName(localPath);
            if (localDir is not null)
            {
                Directory.CreateDirectory(localDir);
            }

            if (content is TextResourceContents text)
            {
                await File.WriteAllTextAsync(localPath, text.Text, cancellationToken);
            }
            else if (content is BlobResourceContents blob)
            {
                var bytes = Convert.FromBase64String(blob.Blob);
                await File.WriteAllBytesAsync(localPath, bytes, cancellationToken);
            }
        }

        return skillDir;
    }

    /// <summary>
    /// Download all available skills from a server to a local directory.
    /// Returns the list of downloaded skill directory paths.
    /// </summary>
    public static async Task<IReadOnlyList<string>> SyncSkillsAsync(
        this McpClient client, string targetDirectory,
        bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(targetDirectory);

        var skills = await client.ListSkillsAsync(cancellationToken);
        var downloaded = new List<string>();

        foreach (var skill in skills)
        {
            try
            {
                var path = await client.DownloadSkillAsync(skill.Name, targetDirectory, overwrite, cancellationToken);
                downloaded.Add(path);
            }
            catch (IOException)
            {
                // Silently skip if directory already exists and overwrite is false
            }
        }

        return downloaded;
    }
}
