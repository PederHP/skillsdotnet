using SkillsDotNet.Mcp;

namespace SkillsDotNet.Mcp.Tests;

public class SkillResourceFactoryTests
{
    private static string TestSkillsDir =>
        Path.Combine(AppContext.BaseDirectory, "TestSkills");

    [Fact]
    public void CreateResources_SimpleSkill_CreatesThreeResources()
    {
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "simple-skill"));

        var resources = SkillResourceFactory.CreateResources(skill);

        // SKILL.md, _manifest, and {+path} template
        Assert.Equal(3, resources.Count);
    }

    [Fact]
    public void CreateResources_SkillMdResource_HasCorrectUri()
    {
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "simple-skill"));

        var resources = SkillResourceFactory.CreateResources(skill);

        var mainResource = resources[0];
        Assert.False(mainResource.IsTemplated);
        Assert.NotNull(mainResource.ProtocolResource);
        Assert.Equal("skill://simple-skill/SKILL.md", mainResource.ProtocolResource!.Uri);
    }

    [Fact]
    public void CreateResources_ManifestResource_HasCorrectUri()
    {
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "simple-skill"));

        var resources = SkillResourceFactory.CreateResources(skill);

        var manifestResource = resources[1];
        Assert.False(manifestResource.IsTemplated);
        Assert.NotNull(manifestResource.ProtocolResource);
        Assert.Equal("skill://simple-skill/_manifest", manifestResource.ProtocolResource!.Uri);
    }

    [Fact]
    public void CreateResources_TemplateResource_IsTemplated()
    {
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "simple-skill"));

        var resources = SkillResourceFactory.CreateResources(skill);

        var templateResource = resources[2];
        Assert.True(templateResource.IsTemplated);
    }

    [Fact]
    public void CreateResources_ResourcesMode_ListsAllFiles()
    {
        var skill = SkillDirectoryScanner.ScanSkill(
            Path.Combine(TestSkillsDir, "code-review"));

        var options = new SkillOptions { SupportingFiles = SkillFileMode.Resources };
        var resources = SkillResourceFactory.CreateResources(skill, options);

        // SKILL.md + _manifest + each supporting file (checklist.md)
        // The main SKILL.md is not duplicated in the supporting files list
        Assert.True(resources.Count >= 3);
    }

    [Fact]
    public void CreateResources_NullSkill_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SkillResourceFactory.CreateResources(null!));
    }

    [Fact]
    public void ReadSupportingFile_PathTraversal_Throws()
    {
        var skillDir = Path.Combine(TestSkillsDir, "simple-skill");

        Assert.Throws<ArgumentException>(
            () => SkillResourceFactory.ReadSupportingFile(skillDir, "../code-review/SKILL.md"));
    }

    [Fact]
    public void ReadSupportingFile_AbsolutePath_Throws()
    {
        var skillDir = Path.Combine(TestSkillsDir, "simple-skill");

        Assert.Throws<ArgumentException>(
            () => SkillResourceFactory.ReadSupportingFile(skillDir, "/etc/passwd"));
    }

    [Fact]
    public void ReadSupportingFile_ValidPath_ReturnsContents()
    {
        var skillDir = Path.Combine(TestSkillsDir, "code-review");

        var contents = SkillResourceFactory.ReadSupportingFile(skillDir, "references/checklist.md");

        Assert.IsType<ModelContextProtocol.Protocol.TextResourceContents>(contents);
    }

    [Fact]
    public void DetectMimeType_KnownExtensions()
    {
        Assert.Equal("text/markdown", SkillResourceFactory.DetectMimeType("file.md"));
        Assert.Equal("application/json", SkillResourceFactory.DetectMimeType("file.json"));
        Assert.Equal("text/plain", SkillResourceFactory.DetectMimeType("file.txt"));
        Assert.Equal("image/png", SkillResourceFactory.DetectMimeType("file.png"));
        Assert.Equal("application/octet-stream", SkillResourceFactory.DetectMimeType("file.xyz"));
    }
}
