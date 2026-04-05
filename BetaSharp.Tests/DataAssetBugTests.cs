using System.Reflection;
using System.Text.Json;
using BetaSharp.DataAsset;

namespace BetaSharp.Tests;

public class DataAssetBugTests : IDisposable
{
    private readonly string _tempDir;

    public DataAssetBugTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        // Force finalization of any leaked FileStream handles (Bug 2) before
        // trying to delete the temp directory, otherwise cleanup fails on Windows.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -------------------------------------------------------------------------
    // Bug 1: InvalidOperationException when lazy asset's JSON deserializes to null
    //
    // Root cause: when the JSON file contains "null", FromJson returns null and
    // the lazy resolver throws InvalidOperationException instead of succeeding.
    // -------------------------------------------------------------------------

    [Fact]
    public void Lazy_asset_with_null_json_content_throws_instead_of_recursing()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.json"), "null");

        var id = new ResourceLocation(Namespace.BetaSharp, "test");
        var holder = DataAssetLoader<GameMode>.CreateLazyHolder(_tempDir, id);

        Assert.Throws<InvalidOperationException>(() => _ = holder.Value);
    }

    // -------------------------------------------------------------------------
    // Bug 2: File handle not leaked when JsonSerializer.Deserialize throws
    //
    // The lazy resolver uses a `using` block around FileStream, ensuring the
    // handle is closed even if deserialization throws.
    // -------------------------------------------------------------------------

    [Fact]
    public void File_handle_is_not_leaked_when_json_deserialization_throws_during_lazy_load()
    {
        string jsonPath = Path.Combine(_tempDir, "test.json");
        File.WriteAllText(jsonPath, "{not valid json");

        var id = new ResourceLocation(Namespace.BetaSharp, "test");
        var holder = DataAssetLoader<GameMode>.CreateLazyHolder(_tempDir, id);

        // The lazy resolver reads the file with a `using` block, so even when
        // JsonSerializer throws, the file handle is properly released.
        // We wrap lazy load errors in InvalidOperationException to provide file path context.
        Assert.Throws<InvalidOperationException>(() => _ = holder.Value);

        // On Windows, File.Delete fails with IOException if any open handle does not
        // have FILE_SHARE_DELETE set. If the handle were leaked this would throw.
        File.Delete(jsonPath);
    }

    // -------------------------------------------------------------------------
    // Bug 3: Wrong directory existence check in LoadDatapackAssets (same pattern
    //        repeated in LoadWorldAssets and LoadResourcepackAssets)
    //
    // Root cause: the code checks !Directory.Exists(pack) instead of
    // !Directory.Exists(assets). Because `pack` was just obtained from
    // Directory.EnumerateDirectories it always exists, so the guard never fires
    // and OnLoadAssets is called with the non-existent "data/" path, faulting
    // the internal async task with DirectoryNotFoundException.
    // -------------------------------------------------------------------------

    [Fact]
    public void LoadDatapackAssets_skips_pack_without_data_directory()
    {
        // Create a datapack folder with no "data/" subdirectory inside it.
        string packDir = Path.Combine(_tempDir, "datapacks", "mypack");
        Directory.CreateDirectory(packDir);

        var testLoader = new DataAssetLoader<GameMode>("gamemode", LoadLocations.GameDatapack);

        // Replace s_assetLoaders temporarily so only testLoader is processed,
        // leaving the global GameModesLoader unaffected.
        FieldInfo loadersField = typeof(DataAssetLoader)
            .GetField("s_assetLoaders", BindingFlags.NonPublic | BindingFlags.Static)!;
        var loaders = (List<DataAssetLoader>)loadersField.GetValue(null)!;
        var saved = loaders.ToList();
        loaders.Clear();
        loaders.Add(testLoader);

        try
        {
            DataAssetLoader.LoadDatapackAssets(_tempDir);

            // Correct behavior: a pack with no "data/" directory should be silently skipped.
            // Bug:  check is !Directory.Exists(pack), which is always false because pack
            //       came from EnumerateDirectories → OnLoadAssets is called with the
            //       non-existent "data/" path → LoadAssetsFromFolders faults with
            //       DirectoryNotFoundException → accessing Assets throws AggregateException.
            // Fix:  check is !Directory.Exists(assets) → pack is correctly skipped → no throw.
            _ = testLoader.Assets;
        }
        finally
        {
            loaders.Clear();
            loaders.AddRange(saved);
        }
    }
}
