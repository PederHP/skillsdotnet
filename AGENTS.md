# SkillsDotNet Development Guide

## Overview

SkillsDotNet is a C# library that exposes [agentskills.io](https://agentskills.io) skills as MCP (Model Context Protocol) resources. Skills are directories containing a `SKILL.md` file with YAML frontmatter. The library provides both **server-side** (registering skills) and **client-side** (discovering/downloading skills) APIs.

## Architecture

```
Server-Side Flow:
SkillDirectoryScanner → FrontmatterParser → SkillInfo → SkillResourceFactory → McpServerResource

Client-Side Flow:
McpClient → SkillClientExtensions → skill:// URIs → SkillManifest → Local files
```

**Key components:**
- [FrontmatterParser.cs](src/SkillsDotNet.Mcp/FrontmatterParser.cs) - Custom YAML parser (no external deps) for `---` delimited frontmatter
- [SkillDirectoryScanner.cs](src/SkillsDotNet.Mcp/SkillDirectoryScanner.cs) - Scans directories, computes SHA256 hashes, produces `SkillInfo`
- [SkillResourceFactory.cs](src/SkillsDotNet.Mcp/SkillResourceFactory.cs) - Creates `McpServerResource` instances with `skill://` URIs
- [SkillValidator.cs](src/SkillsDotNet.Mcp/SkillValidator.cs) - Validates names per agentskills.io spec (NFKC normalized, lowercase, no consecutive hyphens)

## URI Convention

Each skill exposes three resources following the FastMCP convention:
| URI Pattern | Purpose |
|-------------|---------|
| `skill://{name}/SKILL.md` | Full markdown content (listed) |
| `skill://{name}/_manifest` | JSON with file list, sizes, hashes |
| `skill://{name}/{+path}` | Supporting files (template) |

## Build & Test

```bash
# Build all targets (.NET 8, 9, 10)
dotnet build

# Run tests
dotnet test

# Test specific framework
dotnet test -f net9.0
```

Tests use xUnit with test fixtures in [tests/SkillsDotNet.Mcp.Tests/TestSkills/](tests/SkillsDotNet.Mcp.Tests/TestSkills/).

## Code Patterns

### Extension Methods on `IMcpServerBuilder`
Server registration uses fluent builder pattern in `Microsoft.Extensions.DependencyInjection` namespace:
```csharp
builder.Services
    .AddMcpServer()
    .WithSkill("/path")           // Single skill
    .WithSkillsDirectory("/dir")  // All subdirs with SKILL.md
    .WithClaudeSkills();          // Vendor shortcut (~/.claude/skills/)
```

### Extension Methods on `McpClient`
Client discovery uses async extensions returning DTOs:
```csharp
var skills = await client.ListSkillsAsync();     // → IReadOnlyList<SkillSummary>
var manifest = await client.GetSkillManifestAsync("name");  // → SkillManifest
```

### Required Init Properties
Use `required` keyword with `init` for model classes:
```csharp
public sealed class SkillInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}
```

### Null Checks
Use `ArgumentNullException.ThrowIfNull()` pattern at method entry.

### Path Security
Always validate paths to prevent traversal attacks:
```csharp
if (Path.IsPathRooted(path) || path.Contains(".."))
    throw new ArgumentException("Invalid path");
```

## Testing Conventions

- Test class names: `{ClassName}Tests`
- Test fixtures copied to output via `<Content Include="TestSkills\**\*" CopyToOutputDirectory="PreserveNewest" />`
- Access test data: `Path.Combine(AppContext.BaseDirectory, "TestSkills", "skill-name")`
- Internal members exposed via `InternalsVisibleTo` in csproj

## Key Validation Rules (agentskills.io spec)

- Skill names: lowercase, alphanumeric + hyphens, no leading/trailing/consecutive hyphens, NFKC normalized, max 64 chars
- Directory name must match `name` field in frontmatter
- Required frontmatter: `name`, `description`
