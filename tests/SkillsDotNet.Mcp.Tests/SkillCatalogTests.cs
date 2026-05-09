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

    [Fact]
    public void OnDependenciesRequired_DefaultsToNull()
    {
        var catalog = new SkillCatalog();

        Assert.Null(catalog.OnDependenciesRequired);
    }

    [Fact]
    public void OnDependenciesRequired_CanBeSet()
    {
        var catalog = new SkillCatalog();

        catalog.OnDependenciesRequired = (request, ct) => Task.FromResult(true);

        Assert.NotNull(catalog.OnDependenciesRequired);
    }

    [Fact]
    public void OnDependenciesReleased_DefaultsToNull()
    {
        var catalog = new SkillCatalog();

        Assert.Null(catalog.OnDependenciesReleased);
    }

    [Fact]
    public void OnDependenciesReleased_CanBeSet()
    {
        var catalog = new SkillCatalog();

        catalog.OnDependenciesReleased = (release, ct) => Task.CompletedTask;

        Assert.NotNull(catalog.OnDependenciesReleased);
    }

    [Fact]
    public void UnloadSkillTool_IsNull_WhenNothingLoaded()
    {
        var catalog = new SkillCatalog();

        Assert.Null(catalog.UnloadSkillTool);
    }

    [Fact]
    public void Tools_ContainsOnlyLoad_WhenNothingLoaded()
    {
        var catalog = new SkillCatalog();

        Assert.Single(catalog.Tools);
        Assert.Equal("load_skill", catalog.Tools[0].Name);
    }

    [Fact]
    public void UnloadSkillTool_BecomesAvailable_WhenSkillIsLoaded()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");

        Assert.NotNull(catalog.UnloadSkillTool);
        Assert.Equal("unload_skill", catalog.UnloadSkillTool!.Name);
    }

    [Fact]
    public void Tools_ContainsBothFunctions_WhenSkillIsLoaded()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");

        Assert.Equal(2, catalog.Tools.Count);
        Assert.Equal("load_skill", catalog.Tools[0].Name);
        Assert.Equal("unload_skill", catalog.Tools[1].Name);
    }

    [Fact]
    public void UnloadSkillTool_ListsLoadedSkillsInDescription()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.RegisterTestSkill("beta", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");
        catalog.MarkLoadedForTesting("beta");

        Assert.Contains("alpha", catalog.UnloadSkillTool!.Description);
        Assert.Contains("beta", catalog.UnloadSkillTool!.Description);
    }

    [Fact]
    public async Task UnloadSkillAsync_ThrowsOnNullName()
    {
        var catalog = new SkillCatalog();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => catalog.UnloadSkillAsync(null!));
    }

    [Fact]
    public async Task UnloadSkillAsync_ThrowsWhenSkillNotLoaded()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => catalog.UnloadSkillAsync("alpha"));
    }

    [Fact]
    public async Task UnloadSkillAsync_ReturnsCapturedToolCallIds()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha", "call_123", "call_456");

        var result = await catalog.UnloadSkillAsync("alpha");

        Assert.Equal("alpha", result.SkillName);
        Assert.Equal(new[] { "call_123", "call_456" }, result.ToolCallIds);
    }

    [Fact]
    public async Task UnloadSkillAsync_RemovesUnloadTool_WhenLastSkillUnloaded()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");

        Assert.NotNull(catalog.UnloadSkillTool);

        await catalog.UnloadSkillAsync("alpha");

        Assert.Null(catalog.UnloadSkillTool);
        Assert.Empty(catalog.LoadedSkillNames);
    }

    [Fact]
    public async Task UnloadSkillAsync_KeepsUnloadTool_WhenOtherSkillsStillLoaded()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.RegisterTestSkill("beta", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");
        catalog.MarkLoadedForTesting("beta");

        await catalog.UnloadSkillAsync("alpha");

        Assert.NotNull(catalog.UnloadSkillTool);
        Assert.Equal(new[] { "beta" }, catalog.LoadedSkillNames);
    }

    [Fact]
    public async Task UnloadSkillAsync_NoDeps_DoesNotInvokeReleaseCallback()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");

        var fired = false;
        catalog.OnDependenciesReleased = (release, ct) =>
        {
            fired = true;
            return Task.CompletedTask;
        };

        var result = await catalog.UnloadSkillAsync("alpha");

        Assert.False(fired);
        Assert.Empty(result.ReleasedServers);
    }

    [Fact]
    public async Task UnloadSkillAsync_FiresReleaseCallback_OnlyForServersNoLongerNeeded()
    {
        var catalog = new SkillCatalog();
        // alpha needs github + slack; beta needs github only.
        catalog.RegisterTestSkill("alpha", new[] { "github", "slack" });
        catalog.RegisterTestSkill("beta", new[] { "github" });
        catalog.MarkLoadedForTesting("alpha");
        catalog.MarkLoadedForTesting("beta");

        SkillDependencyRelease? captured = null;
        catalog.OnDependenciesReleased = (release, ct) =>
        {
            captured = release;
            return Task.CompletedTask;
        };

        var result = await catalog.UnloadSkillAsync("alpha");

        Assert.NotNull(captured);
        Assert.Equal("alpha", captured!.SkillName);
        // github is still needed by beta; only slack should be released.
        Assert.Equal(new[] { "slack" }, captured.ServerNames);
        Assert.Equal(new[] { "slack" }, result.ReleasedServers);
    }

    [Fact]
    public async Task UnloadSkillAsync_AllDepsStillNeeded_DoesNotInvokeReleaseCallback()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", new[] { "github" });
        catalog.RegisterTestSkill("beta", new[] { "github" });
        catalog.MarkLoadedForTesting("alpha");
        catalog.MarkLoadedForTesting("beta");

        var fired = false;
        catalog.OnDependenciesReleased = (release, ct) =>
        {
            fired = true;
            return Task.CompletedTask;
        };

        var result = await catalog.UnloadSkillAsync("alpha");

        Assert.False(fired);
        Assert.Empty(result.ReleasedServers);
    }

    [Fact]
    public async Task UnloadSkillAsync_FiresReleaseCallback_ForAllDeps_WhenLastLoaded()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", new[] { "github", "slack" });
        catalog.MarkLoadedForTesting("alpha");

        SkillDependencyRelease? captured = null;
        catalog.OnDependenciesReleased = (release, ct) =>
        {
            captured = release;
            return Task.CompletedTask;
        };

        await catalog.UnloadSkillAsync("alpha");

        Assert.NotNull(captured);
        Assert.Equal(new[] { "github", "slack" }, captured!.ServerNames);
    }

    [Fact]
    public async Task UnloadSkillTool_Invocation_UnloadsSkillAndReturnsConfirmation()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");

        var unloadTool = catalog.UnloadSkillTool!;
        var args = new Microsoft.Extensions.AI.AIFunctionArguments
        {
            ["skillName"] = "alpha",
        };

        var result = await unloadTool.InvokeAsync(args);
        var resultStr = result?.ToString() ?? string.Empty;

        Assert.Contains("alpha", resultStr);
        Assert.Empty(catalog.LoadedSkillNames);
        Assert.Null(catalog.UnloadSkillTool);
    }
}
