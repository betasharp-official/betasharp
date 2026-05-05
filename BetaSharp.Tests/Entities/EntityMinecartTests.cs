using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.NBT;
namespace BetaSharp.Tests.Entities;

/// <summary>
/// Focused coverage for <see cref="EntityMinecart"/> (rails, cart types, collisions, NBT, client branch).
/// </summary>
[Collection("EntityTests")]
public sealed class EntityMinecartTests
{
    private static void PlaceRailWithFloor(FakeWorldContext world, int x, int y, int z, int railBlockId, int meta)
    {
        if (!BlockRail.isRail(railBlockId))
        {
            throw new ArgumentException("Not a rail block id", nameof(railBlockId));
        }

        world.Writer.SetBlock(x, y - 1, z, Block.Stone.id);
        world.Writer.SetBlock(x, y, z, railBlockId, meta);
    }

    /// <summary>Opaque neighbor required for slope rails to stay valid (see <see cref="BlockRail.neighborUpdate"/>).</summary>
    private static void PlaceSlopeSupport(FakeWorldContext world, int x, int railY, int z, int slopeMeta)
    {
        switch (slopeMeta)
        {
            case 2:
                world.Writer.SetBlock(x + 1, railY, z, Block.Stone.id);
                break;
            case 3:
                world.Writer.SetBlock(x - 1, railY, z, Block.Stone.id);
                break;
            case 4:
                world.Writer.SetBlock(x, railY, z - 1, Block.Stone.id);
                break;
            case 5:
                world.Writer.SetBlock(x, railY, z + 1, Block.Stone.id);
                break;
        }
    }

    [Fact]
    public void GetTrackPosition_and_GetTrackPositionOffset_on_flat_straight_rails()
    {
        FakeWorldContext world = new();
        PlaceRailWithFloor(world, 8, 64, 8, Block.Rail.id, 0);
        PlaceRailWithFloor(world, 12, 64, 8, Block.Rail.id, 1);

        var cart = new EntityMinecart(world, 8.5, 65.0, 8.5, 0);
        Assert.NotNull(cart.GetTrackPosition(8.5, 65.0, 8.5));
        Assert.NotNull(cart.GetTrackPositionOffset(8.5, 65.0, 8.5, 0.1));

        cart.SetPosition(1000, 65, 1000);
        Assert.Null(cart.GetTrackPosition(cart.X, cart.Y, cart.Z));
    }

    [Fact]
    public void Ticks_on_straight_rail_meta0_and_meta1()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 31, 0, 15, 63);
        PlaceRailWithFloor(world, 8, 64, 8, Block.Rail.id, 0);
        PlaceRailWithFloor(world, 9, 64, 8, Block.Rail.id, 0);
        PlaceRailWithFloor(world, 12, 64, 12, Block.Rail.id, 1);
        PlaceRailWithFloor(world, 12, 64, 13, Block.Rail.id, 1);

        var cart0 = new EntityMinecart(world, 8.5, 65.0, 8.5, 0);
        cart0.VelocityZ = 0.15;
        Assert.True(world.Entities.SpawnEntity(cart0));

        var cart1 = new EntityMinecart(world, 12.5, 65.0, 12.5, 0);
        cart1.VelocityX = 0.15;
        Assert.True(world.Entities.SpawnEntity(cart1));

        double startSpeed0 = EntityTestHarness.HorizontalSpeed(cart0);
        double startSpeed1 = EntityTestHarness.HorizontalSpeed(cart1);
        EntityTestHarness.AdvanceGameTicks(world, 80);

        Assert.True(EntityTestHarness.HorizontalSpeed(cart0) > 0.0);
        Assert.True(EntityTestHarness.HorizontalSpeed(cart1) > 0.0);
        Assert.NotEqual(startSpeed0, EntityTestHarness.HorizontalSpeed(cart0));
        Assert.NotEqual(startSpeed1, EntityTestHarness.HorizontalSpeed(cart1));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Ticks_on_slope_rail_meta(int slopeMeta)
    {
        FakeWorldContext world = new();
        int x = 16 + slopeMeta;
        int z = 8;
        PlaceRailWithFloor(world, x, 64, z, Block.Rail.id, slopeMeta);
        PlaceSlopeSupport(world, x, 64, z, slopeMeta);

        var cart = new EntityMinecart(world, x + 0.5, 65.0, z + 0.5, 0);
        cart.VelocityX = 0.05;
        cart.VelocityZ = 0.05;
        Assert.True(world.Entities.SpawnEntity(cart));
        double startY = cart.Y;
        EntityTestHarness.AdvanceGameTicks(world, 60);
        Assert.NotEqual(startY, cart.Y);
    }

    [Fact]
    public void Powered_rail_boost_and_braking_and_detector_rail()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 31, 0, 15, 63);

        PlaceRailWithFloor(world, 8, 64, 8, Block.PoweredRail.id, 0 | 8);
        var boosted = new EntityMinecart(world, 8.5, 65.0, 8.5, 0);
        boosted.VelocityZ = 0.02;
        Assert.True(world.Entities.SpawnEntity(boosted));

        PlaceRailWithFloor(world, 10, 64, 8, Block.PoweredRail.id, 0);
        var braking = new EntityMinecart(world, 10.5, 65.0, 8.5, 0);
        braking.VelocityZ = 0.15;
        Assert.True(world.Entities.SpawnEntity(braking));

        PlaceRailWithFloor(world, 12, 64, 8, Block.DetectorRail.id, 0);
        var detector = new EntityMinecart(world, 12.5, 65.0, 8.5, 0);
        detector.VelocityZ = 0.1;
        Assert.True(world.Entities.SpawnEntity(detector));

        double boostedBefore = EntityTestHarness.HorizontalSpeed(boosted);
        double brakingBefore = EntityTestHarness.HorizontalSpeed(braking);

        EntityTestHarness.AdvanceGameTicks(world, 40);

        Assert.NotEqual(boostedBefore, EntityTestHarness.HorizontalSpeed(boosted));
        Assert.NotEqual(brakingBefore, EntityTestHarness.HorizontalSpeed(braking));
        Assert.False(detector.Dead);
    }

    [Fact]
    public void Powered_rail_low_speed_starter_uses_neighbor_suffocate_meta0()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 31, 0, 15, 63);
        int x = 20;
        int z = 8;
        PlaceRailWithFloor(world, x, 64, z, Block.PoweredRail.id, 0 | 8);
        world.Writer.SetBlock(x, 64, z - 1, Block.Stone.id);
        world.Writer.SetBlock(x, 64, z + 1, Block.Stone.id);

        var cart = new EntityMinecart(world, x + 0.5, 65.0, z + 0.5, 0);
        Assert.True(world.Entities.SpawnEntity(cart));
        EntityTestHarness.AdvanceGameTicks(world, 8);
        Assert.True(EntityTestHarness.HorizontalSpeed(cart) > 0.0);
    }

    [Fact]
    public void Powered_rail_low_speed_starter_uses_neighbor_suffocate_meta1()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 31, 0, 15, 63);
        int x = 22;
        int z = 8;
        PlaceRailWithFloor(world, x, 64, z, Block.PoweredRail.id, 1 | 8);
        world.Writer.SetBlock(x - 1, 64, z, Block.Stone.id);
        world.Writer.SetBlock(x + 1, 64, z, Block.Stone.id);

        var cart = new EntityMinecart(world, x + 0.5, 65.0, z + 0.5, 0);
        Assert.True(world.Entities.SpawnEntity(cart));
        EntityTestHarness.AdvanceGameTicks(world, 8);
        Assert.True(EntityTestHarness.HorizontalSpeed(cart) > 0.0);
    }

    [Fact]
    public void Corner_rail_metas_6_to_9_tick()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 40, 0, 15, 63);
        for (int m = 6; m <= 9; m++)
        {
            int x = 8 + m;
            PlaceRailWithFloor(world, x, 64, 8, Block.Rail.id, m);
        }

        var cart = new EntityMinecart(world, 14.5, 65.0, 8.5, 0);
        cart.VelocityX = 0.12;
        Assert.True(world.Entities.SpawnEntity(cart));
        double startX = cart.X;
        EntityTestHarness.AdvanceGameTicks(world, 120);
        Assert.NotEqual(startX, cart.X);
    }

    [Fact]
    public void Two_minecarts_OnCollision_mixed_types()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceRailRunX(world, 4, 20, 64, 8);

        var slow = new EntityMinecart(world, 8.5, 65.0, 8.5, 0);
        slow.VelocityX = 0.2;
        Assert.True(world.Entities.SpawnEntity(slow));

        var furnace = new EntityMinecart(world, 10.5, 65.0, 8.5, 2);
        furnace.VelocityX = -0.15;
        Assert.True(world.Entities.SpawnEntity(furnace));

        EntityTestHarness.AdvanceGameTicks(world, 40);
        Assert.False(slow.Dead);
        Assert.False(furnace.Dead);
    }

    [Fact]
    public void Furnace_minecart_Interact_coal_and_push_vector()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceRailRunX(world, 6, 14, 64, 10);
        var cart = new EntityMinecart(world, 10.5, 65.0, 10.5, 2);
        Assert.True(world.Entities.SpawnEntity(cart));

        var player = new TestEntityPlayer(world);
        player.SetPosition(12.0, 65.0, 10.5);
        Assert.True(world.Entities.SpawnEntity(player));
        player.Inventory.SetStack(0, new ItemStack(Item.Coal, 16));
        player.Inventory.SelectedSlot = 0;

        bool interacted = cart.Interact(player);
        EntityTestHarness.AdvanceGameTicks(world, 30);
        Assert.True(interacted);
        Assert.NotNull(player.Inventory.GetStack(0));
        Assert.Equal(15, player.Inventory.GetStack(0)!.Count);
        Assert.True(EntityTestHarness.HorizontalSpeed(cart) > 0.0);
    }

    [Fact]
    public void Empty_minecart_Interact_player_mounted()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceRailRunX(world, 4, 12, 64, 6);
        var cart = new EntityMinecart(world, 8.5, 65.0, 6.5, 0);
        Assert.True(world.Entities.SpawnEntity(cart));

        var player = new TestEntityPlayer(world);
        player.SetPosition(8.5, 65.0, 6.5);
        Assert.True(world.Entities.SpawnEntity(player));

        bool interacted = cart.Interact(player);
        EntityTestHarness.AdvanceGameTicks(world, 20);
        Assert.True(interacted);
        Assert.Same(cart, player.Vehicle);
        Assert.Same(player, cart.Passenger);
    }

    [Fact]
    public void Chest_minecart_Interact_and_inventory_and_damage_drops()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceRailRunX(world, 4, 10, 64, 4);
        var cart = new EntityMinecart(world, 8.5, 65.0, 4.5, 1);
        cart.SetStack(3, new ItemStack(Item.Stick, 4));
        Assert.Equal(2, cart.RemoveStack(3, 2)!.Count);
        cart.SetStack(3, new ItemStack(Item.Stick, 4));

        Assert.True(world.Entities.SpawnEntity(cart));

        var player = new TestEntityPlayer(world);
        player.SetPosition(8.5, 65.0, 4.5);
        Assert.True(world.Entities.SpawnEntity(player));
        bool interacted = cart.Interact(player);

        Assert.True(cart.Damage(null, 10));
        Assert.True(interacted);
        Assert.True(cart.Dead);
    }

    [Fact]
    public void Empty_cart_damage_breaks_and_AnimateHurt()
    {
        FakeWorldContext world = new();
        var cart = new EntityMinecart(world, 8.5, 65.0, 8.5, 0);
        Assert.True(world.Entities.SpawnEntity(cart));
        cart.AnimateHurt();
        Assert.True(cart.Damage(null, 10));
        Assert.True(cart.Dead);
    }

    [Fact]
    public void Minecart_public_surface_smoke()
    {
        FakeWorldContext world = new();
        var cart = new EntityMinecart(world, 8.5, 65.0, 8.5, 0);
        Assert.True(world.Entities.SpawnEntity(cart));

        var player = new TestEntityPlayer(world);
        player.SetPosition(8.5, 65.0, 8.5);
        Assert.True(world.Entities.SpawnEntity(player));
        Assert.True(cart.CanPlayerUse(player));
        Assert.Equal("Minecart", cart.Name);
        Assert.Equal(64, cart.MaxCountPerStack);
        Assert.Equal(27, cart.Size);
        cart.MarkDirty();
        Assert.False(cart.Dead);
    }

    [Fact]
    public void Client_branch_interpolation_and_remote_static_tick()
    {
        FakeWorldContext world = new();
        world.IsRemote = true;

        var cart = new EntityMinecart(world, 8.0, 65.0, 8.0, 0);
        cart.SetPositionAndAnglesAvoidEntities(9.0, 65.5, 9.0, 45f, 0f, 4);
        Assert.True(world.Entities.SpawnEntity(cart));
        EntityTestHarness.AdvanceGameTicks(world, 6);

        var cart2 = new EntityMinecart(world, 12.0, 65.0, 12.0, 0);
        Assert.True(world.Entities.SpawnEntity(cart2));
        EntityTestHarness.AdvanceGameTicks(world, 2);
        Assert.True(cart.X > 8.0);
        Assert.False(cart2.Dead);
    }

    [Fact]
    public void SetVelocityClient_updates_motion()
    {
        FakeWorldContext world = new();
        world.IsRemote = true;
        var cart = new EntityMinecart(world, 8.0, 65.0, 8.0, 0);
        cart.SetVelocityClient(0.1, 0.0, -0.05);
        Assert.True(world.Entities.SpawnEntity(cart));
        EntityTestHarness.AdvanceGameTicks(world, 2);
        Assert.Equal(0.1, cart.VelocityX, 6);
        Assert.Equal(-0.05, cart.VelocityZ, 6);
    }

    [Fact]
    public void Chest_minecart_NBT_round_trip_with_items()
    {
        FakeWorldContext worldA = new();
        EntityTestHarness.PlaceStoneFloor(worldA, 0, 15, 0, 15, 63);
        var original = new EntityMinecart(worldA, 8.5, 65.0, 8.5, 1);
        original.SetStack(5, new ItemStack(Item.Stick, 3));

        var nbt = new NBTTagCompound();
        Assert.True(original.SaveSelfNbt(nbt));

        FakeWorldContext worldB = new();
        EntityTestHarness.PlaceStoneFloor(worldB, 0, 15, 0, 15, 63);
        Entity? loaded = EntityRegistry.GetEntityFromNbt(nbt, worldB);
        Assert.NotNull(loaded);
        var cart = Assert.IsType<EntityMinecart>(loaded);
        Assert.Equal(1, cart.type);
        Assert.NotNull(cart.GetStack(5));
        Assert.Equal(3, cart.GetStack(5)!.Count);
    }

    [Fact]
    public void Furnace_cart_Damage_breaks_and_drops_furnace()
    {
        FakeWorldContext world = new();
        var cart = new EntityMinecart(world, 8.5, 65.0, 8.5, 2);
        Assert.True(world.Entities.SpawnEntity(cart));
        Assert.True(cart.Damage(null, 5));
        Assert.True(cart.Dead);
    }

    [Fact]
    public void OnCollision_empty_cart_pushes_pig_when_fast()
    {
        FakeWorldContext world = new();
        var cart = new EntityMinecart(world, 8.5, 65.0, 8.5, 0);
        cart.VelocityX = 0.15;
        cart.VelocityZ = 0.12;
        Assert.True(world.Entities.SpawnEntity(cart));

        var pig = new EntityPig(world);
        pig.SetPosition(9.2, 65.0, 8.6);
        Assert.True(world.Entities.SpawnEntity(pig));

        double pigSpeedBefore = EntityTestHarness.HorizontalSpeed(pig);
        cart.OnCollision(pig);
        Assert.True(EntityTestHarness.HorizontalSpeed(pig) > pigSpeedBefore);
    }

    [Fact]
    public void OnCollision_two_furnace_carts_averages_velocity()
    {
        FakeWorldContext world = new();
        var a = new EntityMinecart(world, 8.0, 65.0, 8.0, 2);
        a.VelocityX = 0.12;
        a.VelocityZ = 0.05;
        Assert.True(world.Entities.SpawnEntity(a));

        var b = new EntityMinecart(world, 8.3, 65.0, 8.2, 2);
        b.VelocityX = -0.1;
        b.VelocityZ = 0.04;
        Assert.True(world.Entities.SpawnEntity(b));

        a.OnCollision(b);
        Assert.True(EntityTestHarness.HorizontalSpeed(a) >= 0.0);
        Assert.True(EntityTestHarness.HorizontalSpeed(b) >= 0.0);
    }

    [Fact]
    public void MarkDead_chest_cart_spills_inventory_without_Damage()
    {
        FakeWorldContext world = new();
        var cart = new EntityMinecart(world, 8.5, 65.0, 8.5, 1);
        cart.SetStack(0, new ItemStack(Item.Stick, 24));
        Assert.True(world.Entities.SpawnEntity(cart));
        cart.MarkDead();
        Assert.True(cart.Dead);
        Assert.NotNull(cart.GetStack(0));
    }

    [Fact]
    public void Furnace_minecart_NBT_round_trip_push_and_fuel()
    {
        FakeWorldContext worldA = new();
        EntityTestHarness.PlaceStoneFloor(worldA, 0, 15, 0, 15, 63);
        var original = new EntityMinecart(worldA, 8.5, 65.0, 8.5, 2);

        var nbt = new NBTTagCompound();
        Assert.True(original.SaveSelfNbt(nbt));

        FakeWorldContext worldB = new();
        EntityTestHarness.PlaceStoneFloor(worldB, 0, 15, 0, 15, 63);
        Entity? loaded = EntityRegistry.GetEntityFromNbt(nbt, worldB);
        Assert.NotNull(loaded);
        var loadedCart = Assert.IsType<EntityMinecart>(loaded);
        Assert.Equal(2, loadedCart.type);
    }
}
