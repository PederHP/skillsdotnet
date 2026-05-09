using System.Collections.Concurrent;
using Anthropic;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using SkillsDotNet.Mcp;

// --- Configuration ---

// Server configs the client host knows about. In a real app this might come from
// a config file, user settings, or a registry. The key point is that the client host
// owns this mapping — the skill library only communicates server names.
var serverConfigs = new Dictionary<string, Func<Task<McpClient>>>
{
    ["everything-server"] = () => McpClient.CreateAsync(
        new StdioClientTransport(new()
        {
            Name = "everything-server",
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-everything"],
        })),
};

var serverProject = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkillServer"));

var model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-sonnet-4-6";

// --- Track dynamically connected MCP servers ---

var connectedServers = new ConcurrentDictionary<string, McpClient>();

// --- Connect to skill server ---

Console.WriteLine($"Connecting to skill server: {serverProject}");

await using var skillClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "SkillServer",
        Command = "dotnet",
        Arguments = ["run", "--no-launch-profile", "--project", serverProject],
    }));

// --- Build skill catalog with dependency callback ---

var catalog = await SkillCatalog.CreateAsync(skillClient);


// We need to create this so it will be in scope for the callback
var options = new ChatOptions { };

// Rebuilds options.Tools from the current catalog + connected-servers state.
// Called at every turn AND inside the dependency callbacks, so newly-connected
// server tools become visible to the model on the very next round-trip within
// the same turn (FunctionInvokingChatClient re-reads options.Tools each iteration).
async Task RefreshToolsAsync()
{
    var tools = new List<AITool>(catalog.Tools);
    foreach (var (_, client) in connectedServers)
    {
        var mcpTools = await client.ListToolsAsync();
        tools.AddRange(mcpTools);
    }
    options.Tools = tools;
}

// This is the key part: when a skill is loaded that declares dependencies,
// the callback fires and the client host connects to the required MCP servers.
catalog.OnDependenciesRequired = async (request, cancellationToken) =>
{
    Console.WriteLine($"\n[host] Skill '{request.SkillName}' requires MCP servers: {string.Join(", ", request.ServerNames)}");

    foreach (var serverName in request.ServerNames)
    {
        if (connectedServers.ContainsKey(serverName))
        {
            Console.WriteLine($"[host] '{serverName}' is already connected.");
            continue;
        }

        if (!serverConfigs.TryGetValue(serverName, out var factory))
        {
            Console.WriteLine($"[host] Unknown server '{serverName}' — cannot connect.");
            return false;
        }

        Console.WriteLine($"[host] Connecting to '{serverName}'...");
        try
        {
            var client = await factory();
            connectedServers[serverName] = client;
            Console.WriteLine($"[host] Connected to '{serverName}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[host] Failed to connect to '{serverName}': {ex.Message}");
            return false;
        }
    }

    // Make the newly-connected server's tools visible to the model in the next
    // round-trip of this same turn — without this, the model sees load_skill
    // succeed but the server's tools are still missing until the next user turn.
    await RefreshToolsAsync();

    return true;
};

// Symmetric to OnDependenciesRequired: the catalog computes which servers are no
// longer needed by *any* still-loaded skill, and we only need to tear those down.
catalog.OnDependenciesReleased = async (release, cancellationToken) =>
{
    Console.WriteLine($"\n[host] Skill '{release.SkillName}' unloaded; releasing MCP servers: {string.Join(", ", release.ServerNames)}");

    foreach (var serverName in release.ServerNames)
    {
        if (!connectedServers.TryRemove(serverName, out var client))
        {
            continue;
        }

        Console.WriteLine($"[host] Disconnecting from '{serverName}'...");
        try
        {
            await client.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[host] Error disconnecting from '{serverName}': {ex.Message}");
        }
    }

    // Drop the disconnected servers' tools from options.Tools immediately, so
    // the model can't keep calling them after the unload in the same turn.
    await RefreshToolsAsync();
};

Console.WriteLine($"Discovered {catalog.SkillNames.Count} skill(s):");
foreach (var name in catalog.SkillNames)
{
    Console.WriteLine($"  {name}");
}
Console.WriteLine();

// --- Set up chat client with function invocation ---

using IChatClient chatClient = new AnthropicClient()
    .AsIChatClient(model)
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

// --- System message with skill context ---

List<ChatMessage> messages =
[
    new(ChatRole.System,
    [
        new TextContent(
            "You are a helpful assistant. " +
            "When the user asks you to perform a task that matches an available skill, " +
            "use the load_skill tool to load the full skill instructions before proceeding. " +
            "After a skill is loaded, its required MCP servers will be connected automatically " +
            "and their tools will become available to you. " +
            "It is very important you do not fake function calls - if the expected tools are missing, " +
            "it means the server dependencies were not loaded and you should ask the user to clarify or try a different task. " +
            "The following skills are available:"),
        .. catalog.GetSkillContexts()
    ]),
];

// --- Chat loop ---

Console.WriteLine("Chat started. Type a message or Ctrl+C to exit.");
Console.WriteLine("Try: \"Explore the everything server and show me what tools it has.\"");
Console.WriteLine();

try
{
    while (true)
    {
        Console.Write("Q: ");
        var input = Console.ReadLine();
        if (input is null)
            break;

        messages.Add(new(ChatRole.User, input));

        await RefreshToolsAsync();

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options))
        {
            Console.Write(update);
            updates.Add(update);
        }
        Console.WriteLine();
        Console.WriteLine();

        messages.AddMessages(updates);
    }
}
finally
{
    // Clean up dynamically connected servers
    foreach (var (name, client) in connectedServers)
    {
        Console.WriteLine($"[host] Disconnecting from '{name}'...");
        await client.DisposeAsync();
    }
}
