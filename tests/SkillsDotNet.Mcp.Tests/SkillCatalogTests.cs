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
    public void OnSkillUnloaded_DefaultsToNull()
    {
        var catalog = new SkillCatalog();

        Assert.Null(catalog.OnSkillUnloaded);
    }

    [Fact]
    public void OnSkillUnloaded_CanBeSet()
    {
        var catalog = new SkillCatalog();

        catalog.OnSkillUnloaded = (result, ct) => Task.CompletedTask;

        Assert.NotNull(catalog.OnSkillUnloaded);
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
    public async Task UnloadSkillAsync_FiresCallback_WithEmptyReleasedServers_WhenSkillHasNoDeps()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");

        SkillUnloadResult? captured = null;
        catalog.OnSkillUnloaded = (result, ct) =>
        {
            captured = result;
            return Task.CompletedTask;
        };

        var result = await catalog.UnloadSkillAsync("alpha");

        Assert.NotNull(captured);
        Assert.Equal("alpha", captured!.SkillName);
        Assert.Empty(captured.ReleasedServers);
        Assert.Empty(result.ReleasedServers);
    }

    [Fact]
    public async Task UnloadSkillAsync_SwallowsCallbackFailures_AndRebuildsTools()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", Array.Empty<string>());
        catalog.RegisterTestSkill("beta", Array.Empty<string>());
        catalog.MarkLoadedForTesting("alpha");
        catalog.MarkLoadedForTesting("beta");

        catalog.OnSkillUnloaded = (_, _) => throw new InvalidOperationException("boom");

        var result = await catalog.UnloadSkillAsync("alpha");

        Assert.Equal("alpha", result.SkillName);
        Assert.Equal(new[] { "beta" }, catalog.LoadedSkillNames);
        Assert.NotNull(catalog.UnloadSkillTool);
        Assert.Contains("beta", catalog.UnloadSkillTool!.Description);
        Assert.DoesNotContain("alpha", catalog.UnloadSkillTool.Description);
    }

    [Fact]
    public async Task UnloadSkillAsync_OnlyReleasesServersNoLongerNeeded()
    {
        var catalog = new SkillCatalog();
        // alpha needs github + slack; beta needs github only.
        catalog.RegisterTestSkill("alpha", new[] { "github", "slack" });
        catalog.RegisterTestSkill("beta", new[] { "github" });
        catalog.MarkLoadedForTesting("alpha");
        catalog.MarkLoadedForTesting("beta");

        SkillUnloadResult? captured = null;
        catalog.OnSkillUnloaded = (result, ct) =>
        {
            captured = result;
            return Task.CompletedTask;
        };

        var result = await catalog.UnloadSkillAsync("alpha");

        Assert.NotNull(captured);
        Assert.Equal("alpha", captured!.SkillName);
        // github is still needed by beta; only slack should be released.
        Assert.Equal(new[] { "slack" }, captured.ReleasedServers);
        Assert.Equal(new[] { "slack" }, result.ReleasedServers);
    }

    [Fact]
    public async Task UnloadSkillAsync_FiresCallback_WithEmptyReleasedServers_WhenAllDepsStillNeeded()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", new[] { "github" });
        catalog.RegisterTestSkill("beta", new[] { "github" });
        catalog.MarkLoadedForTesting("alpha");
        catalog.MarkLoadedForTesting("beta");

        SkillUnloadResult? captured = null;
        catalog.OnSkillUnloaded = (result, ct) =>
        {
            captured = result;
            return Task.CompletedTask;
        };

        var result = await catalog.UnloadSkillAsync("alpha");

        Assert.NotNull(captured);
        Assert.Empty(captured!.ReleasedServers);
        Assert.Empty(result.ReleasedServers);
    }

    [Fact]
    public async Task UnloadSkillAsync_ReleasesAllDeps_WhenLastLoaded()
    {
        var catalog = new SkillCatalog();
        catalog.RegisterTestSkill("alpha", new[] { "github", "slack" });
        catalog.MarkLoadedForTesting("alpha");

        SkillUnloadResult? captured = null;
        catalog.OnSkillUnloaded = (result, ct) =>
        {
            captured = result;
            return Task.CompletedTask;
        };

        await catalog.UnloadSkillAsync("alpha");

        Assert.NotNull(captured);
        Assert.Equal(new[] { "github", "slack" }, captured!.ReleasedServers);
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
