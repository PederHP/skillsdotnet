using System.Reflection;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace SkillsDotNet.Mcp;

/// <summary>
/// Client-side service that discovers skills from one or more MCP servers,
/// caches their frontmatter as <see cref="TextContent"/> context blocks, and
/// exposes <see cref="LoadSkillTool"/>/<see cref="UnloadSkillTool"/> for on-demand
/// skill loading and unloading.
/// </summary>
/// <remarks>
/// The unload tool is only surfaced when at least one skill is currently loaded,
/// so it doesn't clutter context the rest of the time. The accompanying guidance
/// for the model is appended to <c>load_skill</c>'s response — it appears at the
/// moment loading happens and disappears with the tool result on unload.
/// </remarks>
public sealed class SkillCatalog
{
    private readonly Dictionary<string, CachedSkill> _cache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _loadedSkills = new(StringComparer.Ordinal);
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
                Dependencies = frontmatter.GetDependencies(),
            };
        }

        RebuildTools();
    }

    /// <summary>
    /// Removes all skills that were discovered from the given <paramref name="client"/>.
    /// Any of those skills that were currently loaded are also dropped from the loaded set
    /// (without firing <see cref="OnDependenciesReleased"/> — releasing servers belonging
    /// to a torn-down client is the host's responsibility).
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
            _loadedSkills.Remove(key);
        }

        RebuildTools();
    }

    /// <summary>
    /// Names of all discovered skills.
    /// </summary>
    public IReadOnlyList<string> SkillNames => _cache.Keys.ToList();

    /// <summary>
    /// Names of skills that have been loaded via <see cref="LoadSkillAsync"/> and not yet unloaded.
    /// </summary>
    public IReadOnlyList<string> LoadedSkillNames => _loadedSkills.Keys.ToList();

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
    /// Optional callback invoked when a skill with MCP server dependencies is loaded.
    /// The callback receives a <see cref="SkillDependencyRequest"/> describing the required servers.
    /// Return <c>true</c> if all servers are connected, <c>false</c> if any could not be connected.
    /// When <c>false</c> is returned, <see cref="LoadSkillAsync"/> throws <see cref="InvalidOperationException"/>.
    /// If not set, skills with dependencies load silently without notification.
    /// </summary>
    public Func<SkillDependencyRequest, CancellationToken, Task<bool>>? OnDependenciesRequired { get; set; }

    /// <summary>
    /// Optional callback invoked when a skill is unloaded and one or more of its declared
    /// MCP server dependencies are no longer required by any other currently-loaded skill.
    /// The callback receives only the servers that are safe to disconnect.
    /// Best-effort: failures to disconnect do not propagate.
    /// </summary>
    public Func<SkillDependencyRelease, CancellationToken, Task>? OnDependenciesReleased { get; set; }

    /// <summary>
    /// An <see cref="AIFunction"/> that loads a skill's full SKILL.md content by name.
    /// Suitable for use in <c>ChatOptions.Tools</c>.
    /// Rebuilt automatically when clients are added or removed and when the loaded set changes.
    /// </summary>
    public AIFunction LoadSkillTool { get; private set; } = null!;

    /// <summary>
    /// An <see cref="AIFunction"/> that unloads a previously loaded skill, freeing context
    /// and triggering <see cref="OnDependenciesReleased"/> for any servers no longer needed.
    /// <c>null</c> when no skills are currently loaded — in that state there is nothing to unload
    /// and surfacing the tool would just clutter the model's context.
    /// </summary>
    public AIFunction? UnloadSkillTool { get; private set; }

    /// <summary>
    /// Convenience accessor returning the current set of skill-management AIFunctions in a
    /// stable order: <c>load_skill</c> always, <c>unload_skill</c> only when at least one skill
    /// is loaded. Hosts can splat this into their <c>ChatOptions.Tools</c> list each turn.
    /// </summary>
    public IReadOnlyList<AIFunction> Tools =>
        UnloadSkillTool is null
            ? new[] { LoadSkillTool }
            : new[] { LoadSkillTool, UnloadSkillTool };

    /// <summary>
    /// Reads the full SKILL.md content for the given skill from its originating MCP server,
    /// records it as loaded, fires <see cref="OnDependenciesRequired"/> if the skill declares
    /// dependencies, and appends a short instructional postscript reminding the model how to
    /// unload the skill when finished.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The skill name was not found in the catalog.</exception>
    public async Task<string> LoadSkillAsync(
        string skillName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skillName);

        var cached = _cache[skillName];

        if (cached.Dependencies.Count > 0 && OnDependenciesRequired is not null)
        {
            var request = new SkillDependencyRequest
            {
                SkillName = skillName,
                ServerNames = cached.Dependencies,
            };

            var connected = await OnDependenciesRequired(request, cancellationToken);
            if (!connected)
            {
                throw new InvalidOperationException(
                    $"Cannot load skill '{skillName}': required MCP server dependencies could not be satisfied.");
            }
        }

        var result = await cached.Client.ReadResourceAsync(
            cached.ResourceUri, cancellationToken: cancellationToken);

        var content = result.Contents.FirstOrDefault();
        if (content is not TextResourceContents text)
        {
            throw new InvalidOperationException(
                $"No text content returned for skill '{skillName}'.");
        }

        if (!_loadedSkills.TryGetValue(skillName, out var ids))
        {
            ids = new List<string>();
            _loadedSkills[skillName] = ids;
        }

        var callId = TryGetCurrentToolCallId();
        if (callId is not null)
        {
            ids.Add(callId);
        }

        RebuildTools();

        return text.Text + BuildUnloadPostscript(skillName);
    }

    /// <summary>
    /// Marks a previously loaded skill as unloaded. Computes the set of MCP servers that are
    /// no longer required (i.e., this skill's deps minus deps still needed by other loaded
    /// skills) and invokes <see cref="OnDependenciesReleased"/> for that set if non-empty.
    /// </summary>
    /// <returns>
    /// A <see cref="SkillUnloadResult"/> with the captured tool-call IDs (so the host can drop
    /// the corresponding tool results from chat history) and the released server names.
    /// </returns>
    /// <exception cref="KeyNotFoundException">The skill is not currently loaded.</exception>
    public async Task<SkillUnloadResult> UnloadSkillAsync(
        string skillName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skillName);

        if (!_loadedSkills.Remove(skillName, out var callIds))
        {
            throw new KeyNotFoundException(
                $"Skill '{skillName}' is not currently loaded.");
        }

        IReadOnlyList<string> safeToDisconnect = Array.Empty<string>();
        if (_cache.TryGetValue(skillName, out var cached) && cached.Dependencies.Count > 0)
        {
            var stillNeeded = _loadedSkills.Keys
                .Where(_cache.ContainsKey)
                .SelectMany(k => _cache[k].Dependencies)
                .ToHashSet(StringComparer.Ordinal);

            safeToDisconnect = cached.Dependencies
                .Where(d => !stillNeeded.Contains(d))
                .ToList();
        }

        if (safeToDisconnect.Count > 0 && OnDependenciesReleased is not null)
        {
            var release = new SkillDependencyRelease
            {
                SkillName = skillName,
                ServerNames = safeToDisconnect,
            };

            await OnDependenciesReleased(release, cancellationToken);
        }

        RebuildTools();

        return new SkillUnloadResult
        {
            SkillName = skillName,
            ToolCallIds = callIds,
            ReleasedServers = safeToDisconnect,
        };
    }

    private void RebuildTools()
    {
        LoadSkillTool = BuildLoadSkillTool();
        UnloadSkillTool = _loadedSkills.Count == 0 ? null : BuildUnloadSkillTool();
    }

    // Test seam: register a skill in the cache without going through an MCP connection.
    // Used by unit tests that exercise unload semantics (which never call LoadSkillAsync
    // and so never dereference Client). Real consumers go through AddClientAsync.
    internal void RegisterTestSkill(string name, IReadOnlyList<string> dependencies)
    {
        _cache[name] = new CachedSkill
        {
            Client = null!,
            ResourceUri = $"skill://{name}/SKILL.md",
            Frontmatter = new Dictionary<string, object>(),
            Context = new TextContent(string.Empty),
            Dependencies = dependencies,
        };
        RebuildTools();
    }

    // Test seam: mark a registered skill as loaded with the given tool-call IDs,
    // bypassing the resource read. Pairs with RegisterTestSkill.
    internal void MarkLoadedForTesting(string name, params string[] callIds)
    {
        _loadedSkills[name] = callIds.ToList();
        RebuildTools();
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

    private AIFunction BuildUnloadSkillTool()
    {
        var loaded = string.Join(", ", _loadedSkills.Keys);
        return AIFunctionFactory.Create(
            async (string skillName, CancellationToken cancellationToken) =>
            {
                var result = await UnloadSkillAsync(skillName, cancellationToken);
                return result.ReleasedServers.Count > 0
                    ? $"Unloaded '{result.SkillName}'. Released MCP servers: {string.Join(", ", result.ReleasedServers)}."
                    : $"Unloaded '{result.SkillName}'. No MCP servers needed releasing.";
            },
            new AIFunctionFactoryOptions
            {
                Name = "unload_skill",
                Description =
                    "Unload a previously loaded skill to free its context and release any MCP servers " +
                    $"that were connected exclusively for it. Currently loaded: {loaded}",
            });
    }

    private static string BuildUnloadPostscript(string skillName) =>
        $"\n\n---\nWhen you no longer need this skill, call `unload_skill(\"{skillName}\")` " +
        "to free its context and release any MCP servers that were connected for it.";

    /// <summary>
    /// Reads the current tool-call ID from <c>FunctionInvokingChatClient.CurrentContext</c> via
    /// reflection. Returns <c>null</c> if the host isn't using <c>FunctionInvokingChatClient</c>
    /// (e.g., direct SDK calls or a custom invoker), in which case the catalog still tracks the
    /// skill as loaded by name — only the per-call ID is unavailable.
    /// </summary>
    private static string? TryGetCurrentToolCallId()
    {
        try
        {
            var clientType = Type.GetType(
                "Microsoft.Extensions.AI.FunctionInvokingChatClient, Microsoft.Extensions.AI",
                throwOnError: false);
            if (clientType is null) return null;

            var ctx = clientType
                .GetProperty("CurrentContext", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);
            if (ctx is null) return null;

            var callContent = ctx.GetType().GetProperty("CallContent")?.GetValue(ctx);
            if (callContent is null) return null;

            return callContent.GetType().GetProperty("CallId")?.GetValue(callContent) as string;
        }
        catch
        {
            return null;
        }
    }

    private sealed class CachedSkill
    {
        public required McpClient Client { get; init; }
        public required string ResourceUri { get; init; }
        public required IReadOnlyDictionary<string, object> Frontmatter { get; init; }
        public required TextContent Context { get; init; }
        public required IReadOnlyList<string> Dependencies { get; init; }
    }
}
