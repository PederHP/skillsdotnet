using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace SkillsDotNet.Mcp;

/// <summary>
/// Client-side service that discovers skills from one or more MCP servers,
/// caches their frontmatter as <see cref="TextContent"/> context blocks, and
/// exposes a <see cref="LoadSkillTool"/> for on-demand skill loading.
/// </summary>
public sealed class SkillCatalog
{
    private readonly Dictionary<string, CachedSkill> _cache = new(StringComparer.Ordinal);
    private readonly Func<IReadOnlyDictionary<string, object>, string>? _contextFormatter;

    /// <summary>
    /// Creates an empty catalog with an optional context formatter.
    /// Use <see cref="AddClientAsync"/> to populate it with skills.
    /// </summary>
    public SkillCatalog(
        Func<IReadOnlyDictionary<string, object>, string>? contextFormatter = null)
    {
        _contextFormatter = contextFormatter;
        LoadSkillTool = BuildLoadSkillTool();
    }

    /// <summary>
    /// Convenience factory that creates a catalog pre-populated from a single client.
    /// </summary>
    public static async Task<SkillCatalog> CreateAsync(
        McpClient client,
        Func<IReadOnlyDictionary<string, object>, string>? contextFormatter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var catalog = new SkillCatalog(contextFormatter);
        await catalog.AddClientAsync(client, cancellationToken);
        return catalog;
    }

    /// <summary>
    /// Discovers skills from the given <paramref name="client"/>, reads each skill's
    /// <c>SKILL.md</c>, parses frontmatter, and adds them to the catalog.
    /// If a skill name already exists from a different client, it is overwritten.
    /// </summary>
    public async Task AddClientAsync(
        McpClient client, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var skills = await client.ListSkillsAsync(cancellationToken);

        foreach (var skill in skills)
        {
            var result = await client.ReadResourceAsync(skill.Uri, cancellationToken: cancellationToken);
            var content = result.Contents.FirstOrDefault();
            if (content is not TextResourceContents text)
            {
                continue;
            }

            var (frontmatter, _) = FrontmatterParser.Parse(text.Text);
            var context = SkillContextExtensions.ToTextContent(frontmatter, _contextFormatter);

            _cache[skill.Name] = new CachedSkill
            {
                Client = client,
                ResourceUri = skill.Uri,
                Frontmatter = frontmatter,
                Context = context,
            };
        }

        LoadSkillTool = BuildLoadSkillTool();
    }

    /// <summary>
    /// Removes all skills that were discovered from the given <paramref name="client"/>.
    /// </summary>
    public void RemoveClient(McpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var keysToRemove = _cache
            .Where(kvp => kvp.Value.Client == client)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        LoadSkillTool = BuildLoadSkillTool();
    }

    /// <summary>
    /// Names of all discovered skills.
    /// </summary>
    public IReadOnlyList<string> SkillNames => _cache.Keys.ToList();

    /// <summary>
    /// Returns the pre-computed <see cref="TextContent"/> context block for the given skill.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The skill name was not found in the catalog.</exception>
    public TextContent GetSkillContext(string skillName)
    {
        ArgumentNullException.ThrowIfNull(skillName);
        return _cache[skillName].Context;
    }

    /// <summary>
    /// Returns pre-computed <see cref="TextContent"/> context blocks for all discovered skills.
    /// </summary>
    public IReadOnlyList<TextContent> GetSkillContexts()
    {
        return _cache.Values.Select(c => c.Context).ToList();
    }

    /// <summary>
    /// An <see cref="AIFunction"/> that loads a skill's full SKILL.md content by name.
    /// Suitable for use in <c>ChatOptions.Tools</c>.
    /// Rebuilt automatically when clients are added or removed.
    /// </summary>
    public AIFunction LoadSkillTool { get; private set; }

    /// <summary>
    /// Reads the full SKILL.md content for the given skill from its originating MCP server.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The skill name was not found in the catalog.</exception>
    public async Task<string> LoadSkillAsync(
        string skillName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skillName);

        var cached = _cache[skillName];
        var result = await cached.Client.ReadResourceAsync(
            cached.ResourceUri, cancellationToken: cancellationToken);

        var content = result.Contents.FirstOrDefault();
        if (content is TextResourceContents text)
        {
            return text.Text;
        }

        throw new InvalidOperationException(
            $"No text content returned for skill '{skillName}'.");
    }

    private AIFunction BuildLoadSkillTool()
    {
        var names = string.Join(", ", _cache.Keys);
        return AIFunctionFactory.Create(
            (string skillName, CancellationToken cancellationToken) =>
                LoadSkillAsync(skillName, cancellationToken),
            new AIFunctionFactoryOptions
            {
                Name = "load_skill",
                Description = $"Load the full content of a skill by name. Available skills: {names}",
            });
    }

    private sealed class CachedSkill
    {
        public required McpClient Client { get; init; }
        public required string ResourceUri { get; init; }
        public required IReadOnlyDictionary<string, object> Frontmatter { get; init; }
        public required TextContent Context { get; init; }
    }
}
