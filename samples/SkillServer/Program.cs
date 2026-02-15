using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

var skillsPath = Path.Combine(AppContext.BaseDirectory, "skills");

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithSkillsDirectory(skillsPath);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

await builder.Build().RunAsync();
