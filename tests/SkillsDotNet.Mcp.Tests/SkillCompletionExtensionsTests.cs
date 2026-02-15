using Microsoft.Extensions.DependencyInjection;

namespace SkillsDotNet.Mcp.Tests;

public class SkillCompletionExtensionsTests
{
    [Theory]
    [InlineData("skill://code-review/{+path}", "code-review")]
    [InlineData("skill://simple-skill/{+path}", "simple-skill")]
    [InlineData("skill://my-tool/{+path}", "my-tool")]
    public void ParseSkillNameFromTemplateUri_ValidUri_ReturnsName(string uri, string expected)
    {
        var result = SkillCompletionExtensions.ParseSkillNameFromTemplateUri(uri);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://example.com")]
    [InlineData("skill://")]
    [InlineData("skill://name")]
    [InlineData("skill://name/SKILL.md")]
    [InlineData("skill://name/_manifest")]
    [InlineData("skill://name/some/file.md")]
    public void ParseSkillNameFromTemplateUri_InvalidUri_ReturnsNull(string? uri)
    {
        var result = SkillCompletionExtensions.ParseSkillNameFromTemplateUri(uri);
        Assert.Null(result);
    }
}
