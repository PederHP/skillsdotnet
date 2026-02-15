# SkillsDotNet Development Guide

## Overview

SkillsDotNet is a C# library for working with [agentskills.io](https://agentskills.io) skills. Skills are directories containing a `SKILL.md` file with YAML frontmatter. The solution is split into two packages:

- **`SkillsDotNet`** — Core library for parsing, validating, and scanning skills. No external dependencies.
- **`SkillsDotNet.Mcp`** — MCP transport layer that exposes skills as MCP resources. Depends on `SkillsDotNet` and `ModelContextProtocol`.

## Architecture

```
SkillsDotNet (core):
FrontmatterParser → SkillDirectoryScanner → SkillInfo / SkillFileInfo
SkillValidator

SkillsDotNet.Mcp (transport):
Server: SkillInfo → SkillResourceFactory → McpServerResource
Client: McpClient → SkillClientExtensions → skill:// URIs → SkillManifest → Local files
```

**Core components (SkillsDotNet):**
- [FrontmatterParser.cs](src/SkillsDotNet/FrontmatterParser.cs) - Custom YAML parser (no external deps) for `---` delimited frontmatter
- [SkillDirectoryScanner.cs](src/SkillsDotNet/SkillDirectoryScanner.cs) - Scans directories, computes SHA256 hashes, produces `SkillInfo`
- [SkillValidator.cs](src/SkillsDotNet/SkillValidator.cs) - Validates names per agentskills.io spec (NFKC normalized, lowercase, no consecutive hyphens)
- [SkillInfo.cs](src/SkillsDotNet/SkillInfo.cs) - Data model for parsed skills
- [SkillFileInfo.cs](src/SkillsDotNet/SkillFileInfo.cs) - Data model for file entries

**MCP components (SkillsDotNet.Mcp):**
- [SkillResourceFactory.cs](src/SkillsDotNet.Mcp/SkillResourceFactory.cs) - Creates `McpServerResource` instances with `skill://` URIs
- [SkillServerBuilderExtensions.cs](src/SkillsDotNet.Mcp/SkillServerBuilderExtensions.cs) - `IMcpServerBuilder` extensions (`WithSkill`, `WithSkillsDirectory`)
- [VendorSkillExtensions.cs](src/SkillsDotNet.Mcp/VendorSkillExtensions.cs) - Vendor shortcuts (`WithClaudeSkills`, etc.)
- [SkillClientExtensions.cs](src/SkillsDotNet.Mcp/SkillClientExtensions.cs) - `McpClient` extensions for discovery/download

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

Tests use xUnit. Test fixtures live in a shared [tests/TestSkills/](tests/TestSkills/) directory, referenced by both test projects via linked content items.

## Project Structure

```
src/
  SkillsDotNet/              Core library (no external deps)
  SkillsDotNet.Mcp/          MCP transport (depends on SkillsDotNet + ModelContextProtocol)
tests/
  TestSkills/                Shared test fixtures (skill directories)
  SkillsDotNet.Tests/        Tests for the core library
  SkillsDotNet.Mcp.Tests/    Tests for MCP-specific code
```

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
- Shared test fixtures in `tests/TestSkills/`, linked into each test project's output
- Access test data: `Path.Combine(AppContext.BaseDirectory, "TestSkills", "skill-name")`
- Internal members exposed via `InternalsVisibleTo` in csproj

## Key Validation Rules (agentskills.io spec)

- Skill names: lowercase, alphanumeric + hyphens, no leading/trailing/consecutive hyphens, NFKC normalized, max 64 chars
- Directory name must match `name` field in frontmatter
- Required frontmatter: `name`, `description`
