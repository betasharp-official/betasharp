using System.Collections;
using System.Net;
using System.Net.Sockets;
using BetaSharp.Client.Network;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries;
using BetaSharp.Registries.Data;

namespace BetaSharp.Tests;

// ---------------------------------------------------------------------------
// Helpers shared across all three test classes in this file
// ---------------------------------------------------------------------------

/// <summary>
/// Minimal in-memory implementation of <see cref="IReadableRegistry{T}"/> for testing.
/// Keys are built from the entry's <see cref="IDataAsset.Name"/> in the BetaSharp namespace.
/// </summary>
file sealed class StubRegistry<T>(RegistryKey<T> key, IEnumerable<T> items) : IReadableRegistry<T>
    where T : class, IDataAsset
{
    private readonly Dictionary<ResourceLocation, T> _entries =
        items.ToDictionary(item => new ResourceLocation(Namespace.BetaSharp, item.Name));

    public ResourceLocation RegistryKey => key.Location;

    public Holder<T>? Get(ResourceLocation location)
        => _entries.TryGetValue(location, out T? v) ? new Holder<T>(v) : null;

    public T? Get(int id) => null;

    public int GetId(T value) => -1;

    public ResourceLocation? GetKey(T value)
        => _entries.FirstOrDefault(kv => kv.Value == value).Key;

    public bool ContainsKey(ResourceLocation location) => _entries.ContainsKey(location);

    public IEnumerable<ResourceLocation> Keys => _entries.Keys;

    public IEnumerator<T> GetEnumerator() => _entries.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// ---------------------------------------------------------------------------
// BuildSyncPackets / IsReloadable tests — mutate s_dynamicEntries
// ---------------------------------------------------------------------------

[Collection("RegistryAccess")]
public sealed class RegistrySyncTests : IDisposable
{
    private readonly string _tempDir;

    private static readonly RegistryKey<TestEnchantment> s_enchKey =
        new(ResourceLocation.Parse("test:enchantment"));

    public RegistrySyncTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        RegistryAccess.ClearDynamicEntries();
    }

    public void Dispose()
    {
        RegistryAccess.ClearDynamicEntries();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private void WriteBaseEnchantment(string name, int maxLevel)
    {
        string dir = Path.Combine(_tempDir, "assets", "enchantment");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{name}.json"),
            $"{{\"MaxLevel\":{maxLevel}}}");
    }

    [Fact]
    public void BuildSyncPackets_yields_packet_for_reloadable_registry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        var def = new RegistryDefinition<TestEnchantment>(s_enchKey, "enchantment", isReloadable: true);
        RegistryAccess.AddDynamic(def);
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        List<RegistryDataS2CPacket> packets = ra.BuildSyncPackets().ToList();
        Assert.Single(packets);
        Assert.Equal(s_enchKey.Location, packets[0].RegistryId);
    }

    [Fact]
    public void BuildSyncPackets_skips_non_reloadable_registry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        var def = new RegistryDefinition<TestEnchantment>(s_enchKey, "enchantment", isReloadable: false);
        RegistryAccess.AddDynamic(def);
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        List<RegistryDataS2CPacket> packets = ra.BuildSyncPackets().ToList();
        Assert.Empty(packets);
    }

    [Fact]
    public void BuildSyncPackets_packet_contains_entry_for_each_loaded_asset()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteBaseEnchantment("fortune", maxLevel: 3);

        var def = new RegistryDefinition<TestEnchantment>(s_enchKey, "enchantment", isReloadable: true);
        RegistryAccess.AddDynamic(def);
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        RegistryDataS2CPacket packet = ra.BuildSyncPackets().Single();
        IEnumerable<string> names = packet.Entries.Select(e => e.key.Path).OrderBy(n => n);
        Assert.Equal(["fortune", "sharpness"], names);
    }

    [Fact]
    public void BuildSyncPackets_only_reloadable_of_mixed_definitions_are_included()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        var reloadable = new RegistryKey<TestEnchantment>(ResourceLocation.Parse("test:reloadable_ench"));
        var nonReloadable = new RegistryKey<TestEnchantment>(ResourceLocation.Parse("test:baked_ench"));

        RegistryAccess.AddDynamic(
            new RegistryDefinition<TestEnchantment>(reloadable, "enchantment", isReloadable: true));
        RegistryAccess.AddDynamic(
            new RegistryDefinition<TestEnchantment>(nonReloadable, "enchantment", isReloadable: false));

        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        List<ResourceLocation> ids = ra.BuildSyncPackets().Select(p => p.RegistryId!).ToList();
        Assert.Single(ids);
        Assert.Equal(reloadable.Location, ids[0]);
    }

    [Fact]
    public void Rebuild_reloadable_definition_produces_updated_sync_packet()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        var def = new RegistryDefinition<TestEnchantment>(s_enchKey, "enchantment", isReloadable: true);
        RegistryAccess.AddDynamic(def);
        RegistryAccess first = RegistryAccess.Build(basePath: _tempDir);

        WriteBaseEnchantment("fortune", maxLevel: 3);
        RegistryAccess rebuilt = first.Rebuild();

        RegistryDataS2CPacket packet = rebuilt.BuildSyncPackets().Single();
        IEnumerable<string> names = packet.Entries.Select(e => e.key.Path).OrderBy(n => n);
        Assert.Equal(["fortune", "sharpness"], names);
    }
}

// ---------------------------------------------------------------------------
// Packet serialization tests — no global state mutations
// ---------------------------------------------------------------------------

public sealed class PacketSerializationTests
{
    /// <summary>
    /// Creates a connected loopback stream pair for packet round-trip tests.
    /// </summary>
    private static (NetworkStream write, NetworkStream read, Action cleanup) MakeLoopbackPair()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var client = new TcpClient();
        client.Connect((IPEndPoint)listener.LocalEndpoint);
        TcpClient server = listener.AcceptTcpClient();
        listener.Stop();
        return (client.GetStream(), server.GetStream(),
            () => { client.Dispose(); server.Dispose(); }
        );
    }

    // ---- RegistryDataS2CPacket ----

    [Fact]
    public void RegistryDataS2CPacket_Get_sets_registry_id_from_key()
    {
        var mode = new GameMode { Name = "survival" };
        var registry = new StubRegistry<GameMode>(RegistryKeys.GameModes, [mode]);

        RegistryDataS2CPacket packet = RegistryDataS2CPacket.Get(RegistryKeys.GameModes, registry);

        Assert.Equal(RegistryKeys.GameModes.Location, packet.RegistryId);
    }

    [Fact]
    public void RegistryDataS2CPacket_Get_serializes_each_entry_to_json()
    {
        var survival = new GameMode { Name = "survival" };
        var creative = new GameMode { Name = "creative" };
        var registry = new StubRegistry<GameMode>(RegistryKeys.GameModes, [survival, creative]);

        RegistryDataS2CPacket packet = RegistryDataS2CPacket.Get(RegistryKeys.GameModes, registry);

        Assert.Equal(2, packet.Entries.Count);
        Assert.All(packet.Entries, e => Assert.NotNull(e.JsonData));
    }

    [Fact]
    public void RegistryDataS2CPacket_WhenWritingNull_preserves_boolean_false_values()
    {
        // GameMode properties all default to true; creative sets several to false.
        var creative = new GameMode
        {
            Name = "creative",
            DisallowFlying = false,
            FiniteResources = false,
            BlockDrops = false,
            CanBeTargeted = false,
            CanReceiveDamage = false,
            NeedsAir = false,
            BrakeSpeed = 0f
        };
        var registry = new StubRegistry<GameMode>(RegistryKeys.GameModes, [creative]);

        RegistryDataS2CPacket packet = RegistryDataS2CPacket.Get(RegistryKeys.GameModes, registry);

        string json = packet.Entries.Single().JsonData!;
        Assert.Contains("\"DisallowFlying\":false", json);
        Assert.Contains("\"FiniteResources\":false", json);
        Assert.Contains("\"BlockDrops\":false", json);
        Assert.Contains("\"CanBeTargeted\":false", json);
        Assert.Contains("\"CanReceiveDamage\":false", json);
        Assert.Contains("\"NeedsAir\":false", json);
        Assert.Contains("\"BrakeSpeed\":0", json);
    }

    [Fact]
    public void RegistryDataS2CPacket_roundtrip_preserves_registry_id_and_entries()
    {
        var enchantment = new TestEnchantment { Name = "sharpness", MaxLevel = 5, Rarity = "common" };
        var key = new RegistryKey<TestEnchantment>(ResourceLocation.Parse("test:enchantment"));
        var registry = new StubRegistry<TestEnchantment>(key, [enchantment]);

        RegistryDataS2CPacket sent = RegistryDataS2CPacket.Get(key, registry);

        (NetworkStream? writeStream, NetworkStream? readStream, Action? cleanup) = MakeLoopbackPair();
        try
        {
            sent.Write(writeStream);

            var received = new RegistryDataS2CPacket();
            received.Read(readStream);

            Assert.Equal(key.Location, received.RegistryId);
            Assert.Single(received.Entries);
            Assert.Equal("sharpness", received.Entries[0].key.Path);
            Assert.NotNull(received.Entries[0].JsonData);
        }
        finally
        {
            cleanup();
        }
    }

    [Fact]
    public void RegistryDataS2CPacket_roundtrip_null_json_entry_reads_back_as_null()
    {
        // Construct a packet directly via round-trip: pack a null entry by sending
        // known bytes over a loopback stream.
        (NetworkStream? writeStream, NetworkStream? readStream, Action? cleanup) = MakeLoopbackPair();
        try
        {
            // Manually write the packet wire format: registryId, count=1, name, hasData=false
            writeStream.WriteResourceLocation("test:enchantment");
            writeStream.WriteShort(1);
            writeStream.WriteResourceLocation("test:silk_touch");
            writeStream.WriteBoolean(false); // no JSON data

            var received = new RegistryDataS2CPacket();
            received.Read(readStream);

            Assert.Equal("test:enchantment", received.RegistryId);
            Assert.Single(received.Entries);
            Assert.Equal("test:silk_touch", received.Entries[0].key);
            Assert.Null(received.Entries[0].JsonData);
        }
        finally
        {
            cleanup();
        }
    }

    // ---- PlayerGameModeUpdateS2CPacket ----

    [Fact]
    public void PlayerGameModeUpdateS2CPacket_Get_sets_game_mode_name()
    {
        var mode = new GameMode { Name = "creative" };
        PlayerGameModeUpdateS2CPacket packet = PlayerGameModeUpdateS2CPacket.Get(mode);
        Assert.Equal("creative", packet.GameModeName);
    }

    [Fact]
    public void PlayerGameModeUpdateS2CPacket_roundtrip_preserves_name()
    {
        var mode = new GameMode { Name = "survival" };
        PlayerGameModeUpdateS2CPacket sent = PlayerGameModeUpdateS2CPacket.Get(mode);

        (NetworkStream? writeStream, NetworkStream? readStream, Action? cleanup) = MakeLoopbackPair();
        try
        {
            sent.Write(writeStream);

            var received = new PlayerGameModeUpdateS2CPacket();
            received.Read(readStream);

            Assert.Equal("survival", received.GameModeName);
        }
        finally
        {
            cleanup();
        }
    }
}

// ---------------------------------------------------------------------------
// ClientRegistryAccess tests — no global state mutations
// ---------------------------------------------------------------------------

public sealed class ClientRegistryAccessTests
{
    private static readonly RegistryKey<GameMode> s_gameModeKey = RegistryKeys.GameModes;

    private static RegistryDataS2CPacket BuildPacket(params GameMode[] modes)
    {
        var registry = new StubRegistry<GameMode>(s_gameModeKey, modes);
        return RegistryDataS2CPacket.Get(s_gameModeKey, registry);
    }

    [Fact]
    public void Accumulate_and_GetAll_deserializes_all_entries()
    {
        var survival = new GameMode { Name = "survival" };
        var creative = new GameMode { Name = "creative" };

        var access = new ClientRegistryAccess();
        access.Accumulate(BuildPacket(survival, creative));

        IReadOnlyDictionary<ResourceLocation, Holder<GameMode>> all = access.GetAll(s_gameModeKey);
        Assert.Equal(2, all.Count);
        Assert.True(all.ContainsKey("survival"));
        Assert.True(all.ContainsKey("creative"));
    }

    [Fact]
    public void Get_returns_correct_entry_by_name()
    {
        var survival = new GameMode { Name = "survival" };                     // BrakeSpeed = 1f
        var creative = new GameMode { Name = "creative", BrakeSpeed = 0.5f }; // distinct value

        var access = new ClientRegistryAccess();
        access.Accumulate(BuildPacket(survival, creative));

        GameMode? found = access.Get(s_gameModeKey, "creative")?.Value;
        Assert.NotNull(found);
        Assert.Equal(0.5f, found.BrakeSpeed);
    }

    [Fact]
    public void Get_returns_null_for_unknown_name()
    {
        var access = new ClientRegistryAccess();
        access.Accumulate(BuildPacket(new GameMode { Name = "survival" }));

        Assert.Null(access.Get(s_gameModeKey, "spectator"));
    }

    [Fact]
    public void GetAll_for_unknown_registry_returns_empty_dictionary()
    {
        var access = new ClientRegistryAccess();
        // Accumulate nothing for this registry.

        IReadOnlyDictionary<ResourceLocation, Holder<GameMode>> all = access.GetAll(s_gameModeKey);
        Assert.Empty(all);
    }

    [Fact]
    public void Re_accumulate_invalidates_cache_and_reflects_new_data()
    {
        var initial = new GameMode { Name = "survival", BrakeSpeed = 1f };
        var access = new ClientRegistryAccess();
        access.Accumulate(BuildPacket(initial));

        // Force the cache to populate.
        GameMode? first = access.Get(s_gameModeKey, "survival")?.Value;
        Assert.NotNull(first);
        Assert.Equal(1f, first.BrakeSpeed);

        // Re-accumulate with changed data.
        var updated = new GameMode { Name = "survival", BrakeSpeed = 0.5f };
        access.Accumulate(BuildPacket(updated));

        GameMode? second = access.Get(s_gameModeKey, "survival")?.Value;
        Assert.NotNull(second);
        Assert.Equal(0.5f, second.BrakeSpeed);
    }

    [Fact]
    public void Boolean_false_values_survive_full_accumulate_and_get_pipeline()
    {
        // This is the regression test for the WhenWritingNull vs WhenWritingDefault bug:
        // boolean false and float 0 must not be stripped during JSON serialization
        // and must therefore deserialize to false/0 on the client side.
        var creative = new GameMode
        {
            Name = "creative",
            DisallowFlying = false,
            FiniteResources = false,
            BlockDrops = false,
            CanBeTargeted = false,
            CanReceiveDamage = false,
            NeedsAir = false,
            BrakeSpeed = 0f
        };

        var access = new ClientRegistryAccess();
        access.Accumulate(BuildPacket(creative));

        GameMode? result = access.Get(s_gameModeKey, "creative")?.Value;
        Assert.NotNull(result);
        Assert.False(result.DisallowFlying);
        Assert.False(result.FiniteResources);
        Assert.False(result.BlockDrops);
        Assert.False(result.CanBeTargeted);
        Assert.False(result.CanReceiveDamage);
        Assert.False(result.NeedsAir);
        Assert.Equal(0f, result.BrakeSpeed);
    }

    [Fact]
    public void Re_accumulate_without_entry_invalidates_its_holder()
    {
        var survival = new GameMode { Name = "survival" };
        var creative = new GameMode { Name = "creative" };
        var access = new ClientRegistryAccess();
        access.Accumulate(BuildPacket(survival, creative));

        // Retain a holder reference before the resync.
        Holder<GameMode>? holder = access.Get(s_gameModeKey, "creative");
        Assert.NotNull(holder);
        Assert.False(holder.IsInvalid);

        // Resync drops "creative" entirely.
        access.Accumulate(BuildPacket(survival));
        access.GetAll(s_gameModeKey); // trigger the merge

        Assert.True(holder.IsInvalid);
        Assert.Throws<InvalidOperationException>(() => _ = holder.Value);
    }

    [Fact]
    public void Deserialized_entry_has_Name_set_to_the_registry_entry_name()
    {
        var mode = new GameMode { Name = "survival" };
        var access = new ClientRegistryAccess();
        access.Accumulate(BuildPacket(mode));

        GameMode? result = access.Get(s_gameModeKey, "survival")?.Value;
        Assert.NotNull(result);
        Assert.Equal("survival", result.Name);
    }
}
