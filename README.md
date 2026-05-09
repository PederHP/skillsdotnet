# SkillsDotNet

[![NuGet](https://img.shields.io/nuget/v/SkillsDotNet.svg)](https://www.nuget.org/packages/SkillsDotNet)
[![NuGet](https://img.shields.io/nuget/v/SkillsDotNet.Mcp.svg)](https://www.nuget.org/packages/SkillsDotNet.Mcp)

Agent skills ([agentskills.io](https://agentskills.io)) for the C# MCP SDK.

> **Experimental:** This package is under active development and its API is subject to change.

Skills provide "context-as-progressive-disclosure" via `SKILL.md` files. This library uses a `skill://` URI convention to distribute skills as MCP resources -- clients can load frontmatter for discovery (~50-100 tokens), full SKILL.md on demand, and supporting files as needed.

## Installation

```
dotnet add package SkillsDotNet.Mcp
```

Requires the official [C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk) (`ModelContextProtocol` package).

If you only need the core skill types (parsing, validation, scanning) without the MCP transport layer:

```
dotnet add package SkillsDotNet
```

## Server-Side Usage

### Register a single skill directory

```csharp
builder.Services
    .AddMcpServer()
    .WithSkill("/path/to/my-skill");
```

### Register all skills from a directory

Each subdirectory containing a `SKILL.md` is registered as a skill:

```csharp
builder.Services
    .AddMcpServer()
    .WithSkillsDirectory("/path/to/skills");
```

### Vendor shortcuts

Register skills from well-known agent directories:

```csharp
builder.Services
    .AddMcpServer()
    .WithClaudeSkills()    // ~/.claude/skills/
    .WithCursorSkills()    // ~/.cursor/skills/
    .WithCopilotSkills()   // ~/.copilot/skills/
    .WithCodexSkills()     // /etc/codex/skills/ + ~/.codex/skills/
    .WithGeminiSkills()    // ~/.gemini/skills/
    .WithGooseSkills()     // ~/.config/agents/skills/
    .WithOpenCodeSkills(); // ~/.config/opencode/skills/
```

### Options

```csharp
builder.Services
    .AddMcpServer()
    .WithSkillsDirectory("/path/to/skills", new SkillOptions
    {
        // List every file as a separate resource (default: Template)
        SupportingFiles = SkillFileMode.Resources,

        // Custom main file name (default: "SKILL.md")
        MainFileName = "SKILL.md"
    });
```

## Client-Side Usage

### Discover skills on a server

```csharp
var skills = await client.ListSkillsAsync();
foreach (var skill in skills)
{
    Console.WriteLine($"{skill.Name}: {skill.Description}");
}
```

### Read a skill's manifest

```csharp
var manifest = await client.GetSkillManifestAsync("code-review");
foreach (var file in manifest.Files)
{
    Console.WriteLine($"  {file.Path} ({file.Size} bytes, {file.Hash})");
}
```

### Download a skill locally

```csharp
var path = await client.DownloadSkillAsync("code-review", targetDirectory: "./skills");
```

### Sync all skills from a server

```csharp
var paths = await client.SyncSkillsAsync(targetDirectory: "./skills");
```

### Using skills with `IChatClient`

`SkillCatalog` discovers skills from one or more MCP servers and provides two things for use with [`Microsoft.Extensions.AI`](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai): context blocks (frontmatter summaries for the system prompt) and a `load_skill` tool the model can call to load a skill's full content on demand.

```csharp
// Build a catalog from one or more MCP servers
var catalog = new SkillCatalog();
await catalog.AddClientAsync(mcpClient1);
await catalog.AddClientAsync(mcpClient2);

// Get frontmatter context blocks — add to the system message so the
// model knows what skills are available (~50-100 tokens each)
var contexts = catalog.GetSkillContexts(); // IReadOnlyList<TextContent>

var messages = new List<ChatMessage>
{
    new(ChatRole.System, [
        new TextContent("You are a helpful assistant. The following skills are available:"),
        .. contexts
    ]),
    new(ChatRole.User, "Help me review this pull request"),
};

// Add catalog.Tools to ChatOptions.Tools — this is load_skill plus, once any skill
// has been loaded, unload_skill (so the model can free context when finished).
var options = new ChatOptions
{
    Tools = [.. catalog.Tools],
};

var response = await chatClient.GetResponseAsync(messages, options);
```

If a server disconnects, remove its skills from the catalog:

```csharp
catalog.RemoveClient(mcpClient1);
```

### Handling skill dependencies

Skills can declare MCP server dependencies in their frontmatter:

```markdown
---
name: explore-everything
description: Explores an MCP server
dependencies: [everything-server]
---
```

When a skill with dependencies is loaded via the `load_skill` tool, the `OnDependenciesRequired` callback fires so the client host can connect to the required servers on demand:

```csharp
catalog.OnDependenciesRequired = async (request, cancellationToken) =>
{
    Console.WriteLine($"Skill '{request.SkillName}' requires: {string.Join(", ", request.ServerNames)}");

    foreach (var serverName in request.ServerNames)
    {
        // Look up and connect to the server using your own configuration
        var client = await ConnectToServerAsync(serverName, cancellationToken);
        if (client is null)
            return false; // Signal failure — LoadSkillAsync will throw
    }

    return true; // All dependencies satisfied
};
```

The callback receives a `SkillDependencyRequest` with the skill name and the list of required server names. Return `true` when all servers are connected, or `false` to abort (which causes `LoadSkillAsync` to throw `InvalidOperationException`). If the callback is not set, skills with dependencies load silently without notification.

See the [DynamicMcpServers sample](samples/DynamicMcpServers/) for a complete example.

### Unloading skills

Progressive disclosure shouldn't become progressive accumulation. Once at least one skill is loaded, `SkillCatalog.UnloadSkillTool` becomes available alongside `load_skill` — the model is told about it via a short postscript appended to each `load_skill` response, and can call it to free a skill's context when finished:

```csharp
catalog.OnDependenciesReleased = async (release, cancellationToken) =>
{
    // Only fires for servers that are no longer required by any *other* still-loaded
    // skill — the catalog handles ref-counting, so it's safe to disconnect blindly.
    foreach (var serverName in release.ServerNames)
    {
        await DisconnectAsync(serverName, cancellationToken);
    }
};
```

`UnloadSkillAsync` returns a `SkillUnloadResult` with the captured tool-call IDs from the original `load_skill` invocations (when the host uses `Microsoft.Extensions.AI.FunctionInvokingChatClient`), so hosts that want to scrub the chat history can drop the matching tool results. The `unload_skill` AIFunction and its accompanying guidance are only surfaced when at least one skill is currently loaded.

## URI Convention

Each skill exposes three resources following the [FastMCP](https://github.com/jlowin/fastmcp) convention:

| URI | Type | Content |
|-----|------|---------|
| `skill://{name}/SKILL.md` | Resource (listed) | Full SKILL.md content |
| `skill://{name}/_manifest` | Resource (listed) | JSON manifest with file listing |
| `skill://{name}/{+path}` | ResourceTemplate | Supporting files on demand |

### Manifest format

```json
{
  "skill": "code-review",
  "files": [
    { "path": "SKILL.md", "size": 512, "hash": "sha256:abc123..." },
    { "path": "references/checklist.md", "size": 256, "hash": "sha256:def456..." }
  ]
}
```

## Writing a Skill

A skill is a directory containing a `SKILL.md` file with YAML frontmatter:

```
my-skill/
  SKILL.md
  references/
    example.md
```

```markdown
---
name: my-skill
description: What this skill does
license: MIT
compatibility: claude, cursor
metadata:
  author: your-name
  version: "1.0"
---

# My Skill

Instructions for the agent go here.
```

### Frontmatter fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | 1-64 chars, lowercase alphanumeric + hyphens, must match directory name |
| `description` | Yes | 1-1024 chars |
| `license` | No | License identifier |
| `compatibility` | No | Comma-separated list of compatible agents (max 500 chars) |
| `dependencies` | No | List of required MCP server names (connected on demand via `OnDependenciesRequired`) |
| `allowed-tools` | No | Experimental tool restrictions |
| `metadata` | No | Arbitrary key-value pairs for client-specific data |

### Name rules

Per the [agentskills.io spec](https://agentskills.io):
- Lowercase letters, digits, and hyphens only
- No leading, trailing, or consecutive hyphens
- NFKC normalized, max 64 characters
- Must match the directory name

## Packages

| Package | Description |
|---------|-------------|
| `SkillsDotNet` | Core library: `FrontmatterParser`, `SkillValidator`, `SkillDirectoryScanner`, `SkillInfo`, `SkillFileInfo`, `SkillContextExtensions`. Depends on `Microsoft.Extensions.AI.Abstractions`. |
| `SkillsDotNet.Mcp` | MCP transport layer: `SkillResourceFactory`, `SkillCatalog`, builder extensions, client extensions. Depends on `SkillsDotNet` and `ModelContextProtocol`. |

## Target Frameworks

- .NET 10
- .NET 9
- .NET 8

## Acknowledgements

Many conventions and especially the server-side resource implementations in this library are inspired by [FastMCP 3.0](https://github.com/jlowin/fastmcp). We've tried to align with FastMCP whenever possible.

## License

MIT
