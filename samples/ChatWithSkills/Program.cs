using Anthropic;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using SkillsDotNet.Mcp;

// --- Configuration ---

var serverProject = args.Length > 0
    ? args[0]
    : Path.GetFullPath("samples/SkillServer");

var model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-sonnet-4-5";

// --- Connect to skill server ---

Console.WriteLine($"Connecting to skill server: {serverProject}");

await using var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "SkillServer",
        Command = "dotnet",
        Arguments = ["run", "--no-launch-profile", "--project", serverProject],
    }));

// --- Build skill catalog ---

var catalog = await SkillCatalog.CreateAsync(mcpClient);

Console.WriteLine($"Discovered {catalog.SkillNames.Count} skill(s):");
foreach (var name in catalog.SkillNames)
{
    Console.WriteLine($"  {name}");
}
Console.WriteLine();

// --- Set up chat client with function invocation ---
// AnthropicClient reads ANTHROPIC_API_KEY from the environment automatically.

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
            "You are a helpful coding assistant. " +
            "When the user asks you to perform a task that matches an available skill, " +
            "use the load_skill tool to load the full skill instructions before proceeding. " +
            "The following skills are available:"),
        .. catalog.GetSkillContexts()
    ]),
];

var options = new ChatOptions { Tools = [catalog.LoadSkillTool] };

// --- Chat loop ---

Console.WriteLine("Chat started. Type a message or Ctrl+C to exit.");
Console.WriteLine("Try: \"Review this code: public void Login(string password) { if (password == \\\"admin\\\") return; }\"");
Console.WriteLine();

while (true)
{
    Console.Write("Q: ");
    var input = Console.ReadLine();
    if (input is null)
        break;

    messages.Add(new(ChatRole.User, input));

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
