using BetaSharp.Registries;
using BetaSharp.Registries.Data;
using Xunit;

namespace BetaSharp.Tests;

[Collection("RegistryAccess")]
public class AssetLoadErrorTests : IDisposable
{
    private readonly string _tempDir;
    private static readonly RegistryKey<TestEnchantment> s_enchKey = new(ResourceLocation.Parse("test:enchantment"));

    public AssetLoadErrorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        RegistryAccess.ClearDynamicEntries();
    }

    public void Dispose()
    {
        RegistryAccess.ClearDynamicEntries();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private void WriteEnchantment(string name, string content)
    {
        string dir = Path.Combine(_tempDir, "assets", "enchantment");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{name}.json"), content);
    }

    [Fact]
    public void Build_with_syntax_error_throws_AssetLoadException()
    {
        // Valid JSON
        WriteEnchantment("sharpness", "{\"MaxLevel\":5}");
        // Invalid JSON (missing closing brace)
        WriteEnchantment("broken", "{\"MaxLevel\":3");

        var def = new RegistryDefinition<TestEnchantment>(s_enchKey, "enchantment", isReloadable: true);
        RegistryAccess.AddDynamic(def);

        Assert.Throws<AssetLoadException>(() => RegistryAccess.Build(basePath: _tempDir));
    }

    [Fact]
    public void Rebuild_with_syntax_error_throws_and_does_not_affect_original()
    {
        WriteEnchantment("sharpness", "{\"MaxLevel\":5}");
        var def = new RegistryDefinition<TestEnchantment>(s_enchKey, "enchantment", isReloadable: true);
        RegistryAccess.AddDynamic(def);

        RegistryAccess v1 = RegistryAccess.Build(basePath: _tempDir);
        Assert.Equal(5, v1.GetOrThrow(s_enchKey).GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);

        // Introduce a syntax error in a NEW file
        WriteEnchantment("broken", "{\"MaxLevel\":10"); 

        // Rebuild should throw AssetLoadException
        Assert.Throws<AssetLoadException>(() => v1.Rebuild());

        // V1 should still be working and have the old data
        Assert.Equal(5, v1.GetOrThrow(s_enchKey).GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);
    }
}
