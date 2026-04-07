using BetaSharp.Client.Network;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries;
using BetaSharp.Registries.Data;

namespace BetaSharp.Tests;

public class RaceConditionTests
{
    [Fact]
    public void RegistrySync_Invalidates_Removed_Holders_Leading_To_Potential_Crash()
    {
        var registries = new ClientRegistryAccess();
        RegistryKey<GameMode> key = RegistryKeys.GameModes;

        var packet1 = RegistryDataS2CPacket.Get(key, BuildRegistry("survival", "deleted_mode"));
        registries.Accumulate(packet1);

        Holder<GameMode> survivalHolder = registries.Get(key, "survival")!;
        Holder<GameMode> deletedHolder = registries.Get(key, "deleted_mode")!;

        Assert.NotNull(survivalHolder.Value);
        Assert.NotNull(deletedHolder.Value);

        var packet2 = RegistryDataS2CPacket.Get(key, BuildRegistry("survival"));
        registries.Accumulate(packet2);

        _ = registries.Get(key, "survival");

        Assert.True(deletedHolder.IsInvalid);
        Assert.Throws<InvalidOperationException>(() => _ = deletedHolder.Value);
    }

    [Fact]
    public void BundlePacket_Prevents_RaceCondition_By_Updating_State_Atomically()
    {
        var registries = new ClientRegistryAccess();
        RegistryKey<GameMode> key = RegistryKeys.GameModes;

        registries.Accumulate(RegistryDataS2CPacket.Get(key, BuildRegistry("survival", "deleted_mode")));
        Holder<GameMode> initialHolder = registries.Get(key, "deleted_mode")!;
        Holder<GameMode> currentPlayerHolder = initialHolder;

        var bundle = new BundleS2CPacket();

        bundle.Packets.Add(RegistryDataS2CPacket.Get(key, BuildRegistry("survival")));

        var migrationPacket = PlayerGameModeUpdateS2CPacket.Get(new GameMode { Name = "survival", Namespace = Namespace.BetaSharp });
        bundle.Packets.Add(migrationPacket);

        // We simulate the sequential handle calls here.
        foreach (Packet p in bundle.Packets)
        {
            if (p is RegistryDataS2CPacket dp)
            {
                registries.Accumulate(dp);
            }
            if (p is PlayerGameModeUpdateS2CPacket mg)
            {
                // This simulates ClientNetworkHandler.onPlayerGameModeUpdate
                Holder<GameMode> updated = registries.Get(key, mg.GameModeName)!;
                currentPlayerHolder = updated;
            }
        }

        Assert.True(initialHolder.IsInvalid, "The old holder should have been invalidated during the merge.");
        Assert.False(currentPlayerHolder.IsInvalid, "The current holder should be the newly acquired valid one.");
        Assert.Equal("survival", currentPlayerHolder.Value.Name);
    }

    private static DataAssetLoader<GameMode> BuildRegistry(params string[] names)
    {
        var loader = new DataAssetLoader<GameMode>("gamemode", LoadLocations.None, allowUnhandled: false);
        foreach (string name in names)
        {
            var rl = new ResourceLocation(Namespace.BetaSharp, name);
            loader.Assets.Add(rl, new Holder<GameMode>(new GameMode { Name = name }));
        }
        return loader;
    }
}
