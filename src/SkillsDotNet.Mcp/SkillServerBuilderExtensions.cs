using Microsoft.Extensions.DependencyInjection;
using SkillsDotNet.Mcp;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods on <see cref="IMcpServerBuilder"/> for registering agent skills as MCP resources.
/// </summary>
public static class SkillServerBuilderExtensions
{
    /// <summary>
    /// Register a single skill directory as MCP resources.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="skillPath">Path to a directory containing a SKILL.md file.</param>
    /// <param name="options">Optional skill configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMcpServerBuilder WithSkill(
        this IMcpServerBuilder builder,
        string skillPath,
        SkillOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(skillPath);

        options ??= new SkillOptions();

        var skill = SkillDirectoryScanner.ScanSkill(skillPath, options.MainFileName);
        var resources = SkillResourceFactory.CreateResources(skill, options);

        foreach (var resource in resources)
        {
            builder.Services.AddSingleton(resource);
        }

        return builder;
    }

    /// <summary>
    /// Scan a directory for skill subdirectories and register each as MCP resources.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="directoryPath">Path to a directory containing skill subdirectories.</param>
    /// <param name="options">Optional skill configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMcpServerBuilder WithSkillsDirectory(
        this IMcpServerBuilder builder,
        string directoryPath,
        SkillOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(directoryPath);

        options ??= new SkillOptions();

        var skills = SkillDirectoryScanner.ScanDirectory(directoryPath, options.MainFileName);
        foreach (var skill in skills)
        {
            var resources = SkillResourceFactory.CreateResources(skill, options);
            foreach (var resource in resources)
            {
                builder.Services.AddSingleton(resource);
            }
        }

        return builder;
    }

    /// <summary>
    /// Scan multiple directories for skills and register each as MCP resources.
    /// First-wins deduplication: if a skill name appears in multiple directories, only the first is registered.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="directoryPaths">Paths to directories containing skill subdirectories.</param>
    /// <param name="options">Optional skill configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMcpServerBuilder WithSkillsDirectory(
        this IMcpServerBuilder builder,
        IEnumerable<string> directoryPaths,
        SkillOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(directoryPaths);

        options ??= new SkillOptions();
        var registered = new HashSet<string>(StringComparer.Ordinal);

        foreach (var directoryPath in directoryPaths)
        {
            var skills = SkillDirectoryScanner.ScanDirectory(directoryPath, options.MainFileName);
            foreach (var skill in skills)
            {
                if (!registered.Add(skill.Name))
                {
                    continue; // First-wins deduplication
                }

                var resources = SkillResourceFactory.CreateResources(skill, options);
                foreach (var resource in resources)
                {
                    builder.Services.AddSingleton(resource);
                }
            }
        }

        return builder;
    }
}
