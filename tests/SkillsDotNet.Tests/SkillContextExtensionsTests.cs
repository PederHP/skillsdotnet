using SkillsDotNet;

namespace SkillsDotNet.Tests;

public class SkillContextExtensionsTests
{
    [Fact]
    public void DefaultFormatter_ProducesNameColonDescription()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill",
            ["description"] = "A helpful skill",
        };

        var result = SkillContextExtensions.DefaultFormatter(frontmatter);

        Assert.Equal("my-skill: A helpful skill", result);
    }

    [Fact]
    public void DefaultFormatter_HandlesMissingName()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["description"] = "A helpful skill",
        };

        var result = SkillContextExtensions.DefaultFormatter(frontmatter);

        Assert.Equal(": A helpful skill", result);
    }

    [Fact]
    public void DefaultFormatter_HandlesMissingDescription()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill",
        };

        var result = SkillContextExtensions.DefaultFormatter(frontmatter);

        Assert.Equal("my-skill: ", result);
    }

    [Fact]
    public void DefaultFormatter_HandlesMissingBoth()
    {
        var frontmatter = new Dictionary<string, object>();

        var result = SkillContextExtensions.DefaultFormatter(frontmatter);

        Assert.Equal(": ", result);
    }

    [Fact]
    public void ToTextContent_UsesDefaultFormatter()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "test-skill",
            ["description"] = "Does things",
        };

        var content = SkillContextExtensions.ToTextContent(frontmatter);

        Assert.Equal("test-skill: Does things", content.Text);
    }

    [Fact]
    public void ToTextContent_UsesCustomFormatter()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "test-skill",
            ["description"] = "Does things",
        };

        var content = SkillContextExtensions.ToTextContent(
            frontmatter, fm => $"[{fm["name"]}]");

        Assert.Equal("[test-skill]", content.Text);
    }

    [Fact]
    public void ToTextContent_ThrowsOnNullFrontmatter()
    {
        Assert.Throws<ArgumentNullException>(
            () => SkillContextExtensions.ToTextContent(null!));
    }

    [Fact]
    public void AsTextContent_UsesSkillInfoFrontmatter()
    {
        var skill = new SkillInfo
        {
            Name = "my-skill",
            Description = "A helpful skill",
            SkillDirectoryPath = "/tmp/my-skill",
            MainFileName = "SKILL.md",
            Frontmatter = new Dictionary<string, object>
            {
                ["name"] = "my-skill",
                ["description"] = "A helpful skill",
            },
        };

        var content = skill.AsTextContent();

        Assert.Equal("my-skill: A helpful skill", content.Text);
    }

    [Fact]
    public void AsTextContent_UsesCustomFormatter()
    {
        var skill = new SkillInfo
        {
            Name = "my-skill",
            Description = "A helpful skill",
            SkillDirectoryPath = "/tmp/my-skill",
            MainFileName = "SKILL.md",
            Frontmatter = new Dictionary<string, object>
            {
                ["name"] = "my-skill",
                ["description"] = "A helpful skill",
            },
        };

        var content = skill.AsTextContent(fm => $"SKILL:{fm["name"]}");

        Assert.Equal("SKILL:my-skill", content.Text);
    }

    [Fact]
    public void AsTextContent_ThrowsOnNullSkill()
    {
        SkillInfo skill = null!;
        Assert.Throws<ArgumentNullException>(() => skill.AsTextContent());
    }
}
