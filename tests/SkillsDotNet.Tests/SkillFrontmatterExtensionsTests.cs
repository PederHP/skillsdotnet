using SkillsDotNet;

namespace SkillsDotNet.Tests;

public class SkillFrontmatterExtensionsTests
{
    [Fact]
    public void GetDependencies_WithList_ReturnsList()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["dependencies"] = new List<string> { "server-a", "server-b" },
        };

        var deps = frontmatter.GetDependencies();

        Assert.Equal(2, deps.Count);
        Assert.Equal("server-a", deps[0]);
        Assert.Equal("server-b", deps[1]);
    }

    [Fact]
    public void GetDependencies_WithoutField_ReturnsEmpty()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill",
        };

        var deps = frontmatter.GetDependencies();

        Assert.Empty(deps);
    }

    [Fact]
    public void GetDependencies_WithNonListValue_ReturnsEmpty()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["dependencies"] = "single-server",
        };

        var deps = frontmatter.GetDependencies();

        Assert.Empty(deps);
    }

    [Fact]
    public void GetDependencies_ThrowsOnNullFrontmatter()
    {
        IReadOnlyDictionary<string, object> frontmatter = null!;

        Assert.Throws<ArgumentNullException>(() => frontmatter.GetDependencies());
    }

    [Fact]
    public void GetDependencies_OnSkillInfo_ReturnsList()
    {
        var skill = new SkillInfo
        {
            Name = "my-skill",
            Description = "A skill",
            SkillDirectoryPath = "/tmp/my-skill",
            MainFileName = "SKILL.md",
            Frontmatter = new Dictionary<string, object>
            {
                ["dependencies"] = new List<string> { "server-a" },
            },
        };

        var deps = skill.GetDependencies();

        Assert.Single(deps);
        Assert.Equal("server-a", deps[0]);
    }

    [Fact]
    public void GetDependencies_OnSkillInfo_ThrowsOnNull()
    {
        SkillInfo skill = null!;

        Assert.Throws<ArgumentNullException>(() => skill.GetDependencies());
    }
}
