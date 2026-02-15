using SkillsDotNet;
using SkillsDotNet.Mcp;

namespace SkillsDotNet.Mcp.Tests;

public class SkillCompletionRegistryTests
{
    private static string TestSkillsDir =>
        Path.Combine(AppContext.BaseDirectory, "TestSkills");

    [Fact]
    public void GetCompletions_RegisteredSkill_ReturnsSupportingFiles()
    {
        var registry = new SkillCompletionRegistry();
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "code-review"));

        registry.RegisterSkill(skill);

        var (values, total, hasMore) = registry.GetCompletions("code-review", "");

        Assert.NotEmpty(values);
        Assert.Equal(values.Count, total);
        Assert.False(hasMore);
        Assert.Contains("SKILL.md", values);
    }

    [Fact]
    public void GetCompletions_PrefixFilter_ReturnsMatchingFiles()
    {
        var registry = new SkillCompletionRegistry();
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "code-review"));

        registry.RegisterSkill(skill);

        var (values, _, _) = registry.GetCompletions("code-review", "ref");

        Assert.All(values, v => Assert.StartsWith("ref", v));
        Assert.Contains(values, v => v.Contains("checklist.md"));
    }

    [Fact]
    public void GetCompletions_NonMatchingPrefix_ReturnsEmpty()
    {
        var registry = new SkillCompletionRegistry();
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "code-review"));

        registry.RegisterSkill(skill);

        var (values, total, hasMore) = registry.GetCompletions("code-review", "zzz-no-match");

        Assert.Empty(values);
        Assert.Equal(0, total);
        Assert.False(hasMore);
    }

    [Fact]
    public void GetCompletions_UnknownSkill_ReturnsEmpty()
    {
        var registry = new SkillCompletionRegistry();

        var (values, total, hasMore) = registry.GetCompletions("nonexistent", "");

        Assert.Empty(values);
        Assert.Equal(0, total);
        Assert.False(hasMore);
    }

    [Fact]
    public void GetCompletions_SkillWithOnlyMainFile_ReturnsMainFile()
    {
        var registry = new SkillCompletionRegistry();
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "simple-skill"));

        registry.RegisterSkill(skill);

        var (values, total, hasMore) = registry.GetCompletions("simple-skill", "");

        Assert.Single(values);
        Assert.Contains("SKILL.md", values);
        Assert.Equal(1, total);
        Assert.False(hasMore);
    }

    [Fact]
    public void RegisterSkill_NullSkill_Throws()
    {
        var registry = new SkillCompletionRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterSkill(null!));
    }
}
