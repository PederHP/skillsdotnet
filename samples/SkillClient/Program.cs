using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SkillsDotNet;

// Resolve SkillServer project path (run from repo root)
var serverProject = args.Length > 0
    ? args[0]
    : Path.GetFullPath("samples/SkillServer");

Console.WriteLine($"Launching server from: {serverProject}");
Console.WriteLine();

await using var client = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "SkillServer",
        Command = "dotnet",
        Arguments = ["run", "--no-launch-profile", "--project", serverProject],
    }));

// 1. List all resources
Console.WriteLine("=== Listed Resources ===");
var resources = await client.ListResourcesAsync();
foreach (var resource in resources)
{
    Console.WriteLine($"  {resource.Uri}");
    if (!string.IsNullOrEmpty(resource.Description))
        Console.WriteLine($"    {resource.Description}");
}
Console.WriteLine();

// 2. Read SKILL.md
Console.WriteLine("=== skill://code-review/SKILL.md ===");
var skillMd = await client.ReadResourceAsync("skill://code-review/SKILL.md");
var skillText = GetText(skillMd);
Console.WriteLine(skillText);
Console.WriteLine();

// 3. Parse frontmatter from the SKILL.md content
Console.WriteLine("=== Parsed Frontmatter ===");
var (frontmatter, _) = FrontmatterParser.Parse(skillText);
foreach (var (key, value) in frontmatter)
{
    Console.WriteLine($"  {key}: {FormatValue(value)}");
}

Console.WriteLine();

// 4. Read manifest
Console.WriteLine("=== skill://code-review/_manifest ===");
var manifest = await client.ReadResourceAsync("skill://code-review/_manifest");
Console.WriteLine(GetText(manifest));
Console.WriteLine();

// 5. Read supporting file via template URI
Console.WriteLine("=== skill://code-review/references/checklist.md ===");
var checklist = await client.ReadResourceAsync("skill://code-review/references/checklist.md");
Console.WriteLine(GetText(checklist));

static string FormatValue(object value) => value switch
{
    IList<string> list => string.Join(", ", list),
    IDictionary<string, string> dict => string.Join(", ", dict.Select(kv => $"{kv.Key}={kv.Value}")),
    _ => value.ToString() ?? "",
};

static string GetText(ReadResourceResult result)
{
    var content = result.Contents.FirstOrDefault();
    return content is TextResourceContents text
        ? text.Text
        : $"(unexpected content type: {content?.GetType().Name ?? "null"})";
}
