using SkillsDotNet.Mcp;

namespace SkillsDotNet.Mcp.Tests;

public class SkillDirectoryScannerTests
{
    private static string TestSkillsDir =>
        Path.Combine(AppContext.BaseDirectory, "TestSkills");

    [Fact]
    public void ScanSkill_CodeReview_ReturnsCorrectInfo()
    {
        var skillDir = Path.Combine(TestSkillsDir, "code-review");

        var info = SkillDirectoryScanner.ScanSkill(skillDir);

        Assert.Equal("code-review", info.Name);
        Assert.Equal("Perform thorough code reviews with best practices", info.Description);
        Assert.Equal("SKILL.md", info.MainFileName);
        Assert.True(info.Files.Count >= 2); // SKILL.md + references/checklist.md
        Assert.Contains(info.Files, f => f.Path == "SKILL.md");
        Assert.Contains(info.Files, f => f.Path == "references/checklist.md");
    }

    [Fact]
    public void ScanSkill_SimpleSkill_ReturnsCorrectInfo()
    {
        var skillDir = Path.Combine(TestSkillsDir, "simple-skill");

        var info = SkillDirectoryScanner.ScanSkill(skillDir);

        Assert.Equal("simple-skill", info.Name);
        Assert.Equal("A simple test skill", info.Description);
        Assert.Single(info.Files);
        Assert.Equal("SKILL.md", info.Files[0].Path);
    }

    [Fact]
    public void ScanSkill_FileHashesAreSha256()
    {
        var skillDir = Path.Combine(TestSkillsDir, "simple-skill");

        var info = SkillDirectoryScanner.ScanSkill(skillDir);

        foreach (var file in info.Files)
        {
            Assert.StartsWith("sha256:", file.Hash);
            Assert.Equal(64 + 7, file.Hash.Length); // "sha256:" + 64 hex chars
        }
    }

    [Fact]
    public void ScanSkill_FileSizesArePositive()
    {
        var skillDir = Path.Combine(TestSkillsDir, "code-review");

        var info = SkillDirectoryScanner.ScanSkill(skillDir);

        foreach (var file in info.Files)
        {
            Assert.True(file.Size > 0, $"File {file.Path} has size {file.Size}");
        }
    }

    [Fact]
    public void ScanSkill_FrontmatterIsParsed()
    {
        var skillDir = Path.Combine(TestSkillsDir, "code-review");

        var info = SkillDirectoryScanner.ScanSkill(skillDir);

        Assert.Equal("code-review", info.Frontmatter["name"]);
        Assert.Equal("MIT", info.Frontmatter["license"]);
    }

    [Fact]
    public void ScanSkill_NonExistentDirectory_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => SkillDirectoryScanner.ScanSkill("/nonexistent/path"));
    }

    [Fact]
    public void ScanSkill_DirectoryWithoutSkillMd_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            Assert.Throws<FileNotFoundException>(
                () => SkillDirectoryScanner.ScanSkill(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ScanDirectory_FindsAllSkills()
    {
        var skills = SkillDirectoryScanner.ScanDirectory(TestSkillsDir);

        Assert.Equal(2, skills.Count);
        Assert.Contains(skills, s => s.Name == "code-review");
        Assert.Contains(skills, s => s.Name == "simple-skill");
    }

    [Fact]
    public void ScanDirectory_NonExistentDirectory_ReturnsEmpty()
    {
        var skills = SkillDirectoryScanner.ScanDirectory("/nonexistent/path");

        Assert.Empty(skills);
    }

    [Fact]
    public void ScanSkill_FilePathsUsePosixSeparators()
    {
        var skillDir = Path.Combine(TestSkillsDir, "code-review");

        var info = SkillDirectoryScanner.ScanSkill(skillDir);

        foreach (var file in info.Files)
        {
            Assert.DoesNotContain("\\", file.Path);
        }
    }
}
