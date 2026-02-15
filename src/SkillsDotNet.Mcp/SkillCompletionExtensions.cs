using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SkillsDotNet.Mcp;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods on <see cref="IMcpServerBuilder"/> for enabling autocompletion of skill resource template parameters.
/// </summary>
public static class SkillCompletionExtensions
{
    private static readonly object RegistryKey = new();

    /// <summary>
    /// Enables autocompletion for the <c>{+path}</c> parameter in skill resource templates.
    /// Call this after registering skills with <see cref="SkillServerBuilderExtensions.WithSkill"/> or
    /// <see cref="SkillServerBuilderExtensions.WithSkillsDirectory(IMcpServerBuilder, string, SkillOptions?)"/>.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMcpServerBuilder WithSkillCompletions(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var registry = GetOrCreateRegistry(builder);

        builder.WithCompleteHandler((request, cancellationToken) =>
        {
            if (request.Params?.Ref is not ResourceTemplateReference templateRef)
            {
                return new ValueTask<CompleteResult>(new CompleteResult());
            }

            var skillName = ParseSkillNameFromTemplateUri(templateRef.Uri);
            if (skillName is null)
            {
                return new ValueTask<CompleteResult>(new CompleteResult());
            }

            var argumentName = request.Params.Argument.Name;
            if (argumentName != "path")
            {
                return new ValueTask<CompleteResult>(new CompleteResult());
            }

            var prefix = request.Params.Argument.Value;
            var (values, total, hasMore) = registry.GetCompletions(skillName, prefix);

            var result = new CompleteResult
            {
                Completion = new Completion
                {
                    Values = values,
                    Total = total,
                    HasMore = hasMore
                }
            };

            return new ValueTask<CompleteResult>(result);
        });

        return builder;
    }

    /// <summary>
    /// Gets or creates the shared <see cref="SkillCompletionRegistry"/> for the builder.
    /// </summary>
    internal static SkillCompletionRegistry GetOrCreateRegistry(IMcpServerBuilder builder)
    {
        // Use the builder's service collection to store the singleton registry.
        // We look for an existing ServiceDescriptor that holds our registry instance.
        var descriptor = builder.Services.FirstOrDefault(
            d => d.ServiceType == typeof(SkillCompletionRegistry));

        if (descriptor?.ImplementationInstance is SkillCompletionRegistry existing)
        {
            return existing;
        }

        var registry = new SkillCompletionRegistry();
        builder.Services.AddSingleton(registry);
        return registry;
    }

    /// <summary>
    /// Extracts the skill name from a <c>skill://{name}/{+path}</c> template URI.
    /// Returns <c>null</c> if the URI does not match the expected pattern.
    /// </summary>
    internal static string? ParseSkillNameFromTemplateUri(string? uri)
    {
        if (uri is null)
        {
            return null;
        }

        const string prefix = "skill://";
        if (!uri.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        // Expected format: skill://{name}/{+path}
        var rest = uri.AsSpan(prefix.Length);
        var slashIndex = rest.IndexOf('/');
        if (slashIndex <= 0)
        {
            return null;
        }

        var suffix = rest.Slice(slashIndex + 1);
        if (!suffix.SequenceEqual("{+path}".AsSpan()))
        {
            return null;
        }

        return rest.Slice(0, slashIndex).ToString();
    }
}
