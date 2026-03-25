using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;
using Xunit;
using System.IO;
using System.Collections.Generic;

namespace BetaSharp.Tests;

public class TestWorldSaveStorage
{
    [Fact]
    public void TestSavePlayerDataFallback()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "BetaSharpTestWorld");
        if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        
        var storage = new RegionWorldStorage(baseDir, "world", true);
        var props = new WorldProperties(1234, "TestWorld");
        
        // Save dummy player data to players/TestPlayer.dat
        var playersDir = new DirectoryInfo(Path.Combine(baseDir, "world", "players"));
        playersDir.Create();
        var dummyPlayerNbt = new NBTTagCompound();
        dummyPlayerNbt.SetString("TestMarker", "ImHere");
        using (var stream = File.Create(Path.Combine(playersDir.FullName, "TestPlayer.dat")))
        {
            NbtIo.WriteCompressed(dummyPlayerNbt, stream);
        }

        // Save world without players to trigger fallback
        storage.Save(props, new List<EntityPlayer>());

        // Load level.dat and verify player data
        string levelDat = Path.Combine(baseDir, "world", "level.dat");
        using (var stream = File.OpenRead(levelDat))
        {
            var rootTag = NbtIo.ReadCompressed(stream);
            var dataTag = rootTag.GetCompoundTag("Data");
            var playerTag = dataTag.GetCompoundTag("Player");
            Assert.NotNull(playerTag);
            Assert.Equal("ImHere", playerTag.GetString("TestMarker"));
        }
        
        Directory.Delete(baseDir, true);
    }
}
