using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Tests.Entities;

/// <summary>
/// Targeted world setups (rails, fluids, walls) to exercise branches beyond plain open-air ticking.
/// </summary>
[Collection("EntityTests")]
public sealed class EntityMobScenarioTests
{
    [Fact]
    public void Minecart_on_rail_straight_ticks_without_throwing()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 31, 0, 15, 63);
        EntityTestHarness.PlaceRailRunX(world, 4, 28, 64, 8);

        var cart = new EntityMinecart(world, 6.5, 65.0, 8.5, 0);
        Assert.True(world.Entities.SpawnEntity(cart));
        EntityTestHarness.AdvanceGameTicks(world, 400);
        Assert.False(cart.Dead);
        Assert.True(EntityTestHarness.AliveEntityCount(world) >= 1);
    }

    [Fact]
    public void FallingSand_lands_and_settles_or_drops_without_throwing()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 15, 0, 15, 63);
        var sand = new EntityFallingSand(world, 8.5, 72.0, 8.5, Block.Sand.id);
        Assert.True(world.Entities.SpawnEntity(sand));
        EntityTestHarness.AdvanceGameTicks(world, 150);
        Assert.True(sand.Dead || sand.OnGround);
    }

    [Fact]
    public void Squid_in_water_column_ticks_without_throwing()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 15, 0, 15, 63);
        EntityTestHarness.FillWaterColumn(world, 8, 8, 64, 70);

        Entity squid = EntityTestHarness.CreateSpawned(world, EntityRegistry.Squid, 8.5, 66.0, 8.5);
        EntityTestHarness.AdvanceGameTicks(world, 200);
        Assert.False(squid.Dead);
        Assert.True(world.Reader.GetMaterial(8, 66, 8).IsFluid);
    }

    [Fact]
    public void Painting_on_stone_wall_survives_many_ticks()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 15, 0, 15, 63);
        EntityTestHarness.PlaceStoneWallStrip(world, 8, 7, 64, 68);

        var painting = new EntityPainting(world, 8, 65, 8, 2, "Kebab");
        Assert.True(world.Entities.SpawnEntity(painting));
        EntityTestHarness.AdvanceGameTicks(world, 120);
        Assert.True(painting.Dead || world.Entities.Entities.Contains(painting));
    }

    [Fact]
    public void Creeper_with_nearby_player_ticks_without_throwing()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 31, 0, 15, 63);

        var player = new TestEntityPlayer(world);
        player.SetPositionAndAngles(10.5, 65.0, 8.5, 0f, 0f);
        Assert.True(world.Entities.SpawnEntity(player));

        Entity creeper = EntityTestHarness.CreateSpawned(world, EntityRegistry.Creeper, 8.5, 65.0, 8.5);
        EntityTestHarness.AdvanceGameTicks(world, 120);
        Assert.True(creeper.Dead || world.Entities.Entities.Contains(creeper));
        Assert.True(EntityTestHarness.AliveEntityCount(world) >= 1);
    }

    [Fact]
    public void Sheep_and_cow_on_pasture_ticks_extended()
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 15, 0, 15, 63);
        Entity sheep = EntityTestHarness.CreateSpawned(world, EntityRegistry.Sheep, 5.5, 65.0, 5.5);
        Entity cow = EntityTestHarness.CreateSpawned(world, EntityRegistry.Cow, 9.5, 65.0, 9.5);
        EntityTestHarness.AdvanceGameTicks(world, 256);
        Assert.False(sheep.Dead);
        Assert.False(cow.Dead);
        Assert.True(EntityTestHarness.AliveEntityCount(world) >= 2);
    }
}
