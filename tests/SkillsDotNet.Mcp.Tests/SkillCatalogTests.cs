using SkillsDotNet.Mcp;

namespace SkillsDotNet.Mcp.Tests;

public class SkillCatalogTests
{
    [Fact]
    public void Constructor_CreatesEmptyCatalog()
    {
        var catalog = new SkillCatalog();

        Assert.Empty(catalog.SkillNames);
        Assert.Empty(catalog.GetSkillContexts());
        Assert.NotNull(catalog.LoadSkillTool);
        Assert.Equal("load_skill", catalog.LoadSkillTool.Name);
    }

    [Fact]
    public async Task CreateAsync_ThrowsOnNullClient()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => SkillCatalog.CreateAsync(null!));
    }

    [Fact]
    public async Task AddClientAsync_ThrowsOnNullClient()
    {
        var catalog = new SkillCatalog();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => catalog.AddClientAsync(null!));
    }

    [Fact]
    public void RemoveClient_ThrowsOnNullClient()
    {
        var catalog = new SkillCatalog();

        Assert.Throws<ArgumentNullException>(
            () => catalog.RemoveClient(null!));
    }

    [Fact]
    public void GetSkillContext_ThrowsOnUnknownName()
    {
        var catalog = new SkillCatalog();

        Assert.Throws<KeyNotFoundException>(
            () => catalog.GetSkillContext("nonexistent"));
    }

    [Fact]
    public async Task LoadSkillAsync_ThrowsOnUnknownName()
    {
        var catalog = new SkillCatalog();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => catalog.LoadSkillAsync("nonexistent"));
    }
}
