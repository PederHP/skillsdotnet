# SkillsDotNet

Agent skills ([agentskills.io](https://agentskills.io)) for the C# MCP SDK.

Skills provide "context-as-progressive-disclosure" via `SKILL.md` files. This library uses a `skill://` URI convention to distribute skills as MCP resources -- clients can load frontmatter for discovery (~50-100 tokens), full SKILL.md on demand, and supporting files as needed.

## Installation

```
dotnet add package SkillsDotNet.Mcp
```

Requires the official [C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk) (`ModelContextProtocol` package).

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
| `allowed-tools` | No | Experimental tool restrictions |
| `metadata` | No | Arbitrary key-value pairs for client-specific data |

### Name rules

Per the [agentskills.io spec](https://agentskills.io):
- Lowercase letters, digits, and hyphens only
- No leading, trailing, or consecutive hyphens
- NFKC normalized, max 64 characters
- Must match the directory name

## Target Frameworks

- .NET 10
- .NET 9
- .NET 8

## License

MIT
