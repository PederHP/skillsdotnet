using SkillsDotNet.Mcp;

namespace SkillsDotNet.Mcp.Tests;

public class FrontmatterParserTests
{
    [Fact]
    public void Parse_WithValidFrontmatter_ReturnsFrontmatterAndBody()
    {
        var content = "---\nname: my-skill\ndescription: A test skill\n---\n\n# Body\n";

        var (frontmatter, body) = FrontmatterParser.Parse(content);

        Assert.Equal("my-skill", frontmatter["name"]);
        Assert.Equal("A test skill", frontmatter["description"]);
        Assert.StartsWith("# Body", body.TrimStart());
    }

    [Fact]
    public void Parse_WithNoFrontmatter_ReturnsEmptyDictAndOriginalContent()
    {
        var content = "# Just a heading\n\nSome body text.";

        var (frontmatter, body) = FrontmatterParser.Parse(content);

        Assert.Empty(frontmatter);
        Assert.Equal(content, body);
    }

    [Fact]
    public void Parse_WithUnclosedFrontmatter_ReturnsEmptyDictAndOriginalContent()
    {
        var content = "---\nname: my-skill\n# no closing fence\n";

        var (frontmatter, body) = FrontmatterParser.Parse(content);

        Assert.Empty(frontmatter);
        Assert.Equal(content, body);
    }

    [Fact]
    public void Parse_WithQuotedValues_StripsQuotes()
    {
        var content = "---\nname: \"my-skill\"\ndescription: 'A test skill'\n---\n\nBody";

        var (frontmatter, _) = FrontmatterParser.Parse(content);

        Assert.Equal("my-skill", frontmatter["name"]);
        Assert.Equal("A test skill", frontmatter["description"]);
    }

    [Fact]
    public void Parse_WithInlineList_ParsesList()
    {
        var content = "---\ncompatibility: [claude, cursor, copilot]\n---\n\nBody";

        var (frontmatter, _) = FrontmatterParser.Parse(content);

        var list = Assert.IsType<List<string>>(frontmatter["compatibility"]);
        Assert.Equal(["claude", "cursor", "copilot"], list);
    }

    [Fact]
    public void Parse_WithNestedMetadata_ParsesAsDictionary()
    {
        var content = "---\nname: my-skill\nmetadata:\n  author: test\n  version: \"1.0\"\n---\n\nBody";

        var (frontmatter, _) = FrontmatterParser.Parse(content);

        Assert.Equal("my-skill", frontmatter["name"]);
        var metadata = Assert.IsType<Dictionary<string, string>>(frontmatter["metadata"]);
        Assert.Equal("test", metadata["author"]);
        Assert.Equal("1.0", metadata["version"]);
    }

    [Fact]
    public void Parse_SkipsComments()
    {
        var content = "---\n# this is a comment\nname: my-skill\n---\n\nBody";

        var (frontmatter, _) = FrontmatterParser.Parse(content);

        Assert.Single(frontmatter);
        Assert.Equal("my-skill", frontmatter["name"]);
    }

    [Fact]
    public void Parse_WithEmptyContent_ReturnsEmptyDictAndEmptyBody()
    {
        var (frontmatter, body) = FrontmatterParser.Parse("");

        Assert.Empty(frontmatter);
        Assert.Equal("", body);
    }

    [Fact]
    public void Parse_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => FrontmatterParser.Parse(null!));
    }
}
