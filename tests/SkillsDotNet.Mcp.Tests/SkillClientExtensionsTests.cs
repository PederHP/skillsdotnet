using SkillsDotNet.Mcp;

namespace SkillsDotNet.Mcp.Tests;

public class SkillClientExtensionsTests
{
    [Fact]
    public async Task ListSkillsAsync_ThrowsOnNullClient()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => SkillClientExtensions.ListSkillsAsync(null!));
    }

    [Fact]
    public async Task GetSkillManifestAsync_ThrowsOnNullClient()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => SkillClientExtensions.GetSkillManifestAsync(null!, "test"));
    }

    [Fact]
    public async Task DownloadSkillAsync_ThrowsOnNullClient()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => SkillClientExtensions.DownloadSkillAsync(null!, "test", "/tmp"));
    }

    [Fact]
    public async Task SyncSkillsAsync_ThrowsOnNullClient()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => SkillClientExtensions.SyncSkillsAsync(null!, "/tmp"));
    }
}
