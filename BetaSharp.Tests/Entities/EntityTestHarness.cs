using System.Linq;
using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Tests.Entities;

/// <summary>
/// Helpers for entity tests using <see cref="FakeWorldContext"/> (shared with block tests).
/// </summary>
public static class EntityTestHarness
{
    public static double HorizontalSpeed(Entity entity) =>
        Math.Sqrt(entity.VelocityX * entity.VelocityX + entity.VelocityZ * entity.VelocityZ);

    public static int AliveEntityCount(FakeWorldContext world) =>
        world.Entities.Entities.Count(entity => !entity.Dead);

    /// <summary>Fills a horizontal rectangle at <paramref name="floorY"/> with stone so entities have solid ground.</summary>
    public static void PlaceStoneFloor(FakeWorldContext world, int minX, int maxX, int minZ, int maxZ, int floorY)
    {
        int stoneId = Block.Stone.id;
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                world.Writer.SetBlock(x, floorY, z, stoneId);
            }
        }
    }

    /// <summary>Advances simulated world time and runs the full entity tick pass (same order as the game server).</summary>
    public static void AdvanceGameTicks(FakeWorldContext world, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            world.SimulatedWorldTime++;
            world.Entities.TickEntities();
        }
    }

    /// <summary>Creates an entity from the registry, positions it above the floor, and registers it with <see cref="EntityManager"/>.</summary>
    public static Entity CreateSpawned(FakeWorldContext world, EntityType type, double x, double y, double z)
    {
        Entity entity = type.Create(world);
        entity.SetPositionAndAngles(x, y, z, 0f, 0f);
        if (!world.Entities.SpawnEntity(entity))
        {
            throw new InvalidOperationException($"SpawnEntity failed for {type.Id}.");
        }

        return entity;
    }

    /// <summary>Places flat rails along Z at <paramref name="railY"/> (inclusive X range).</summary>
    public static void PlaceRailRunX(FakeWorldContext world, int x0, int x1, int railY, int z)
    {
        int railId = Block.Rail.id;
        int step = x0 <= x1 ? 1 : -1;
        for (int x = x0; x != x1 + step; x += step)
        {
            world.Writer.SetBlock(x, railY, z, railId);
        }
    }

    /// <summary>Fills an inclusive Y column with stationary water (for squid / fluid tests).</summary>
    public static void FillWaterColumn(FakeWorldContext world, int x, int z, int yMin, int yMax)
    {
        int waterId = Block.Water.id;
        for (int y = yMin; y <= yMax; y++)
        {
            world.Writer.SetBlock(x, y, z, waterId);
        }
    }

    /// <summary>Builds a short stone wall segment used as a painting backing (single-column Kebab-sized).</summary>
    public static void PlaceStoneWallStrip(FakeWorldContext world, int x, int z, int yMin, int yMax)
    {
        int stoneId = Block.Stone.id;
        for (int y = yMin; y <= yMax; y++)
        {
            world.Writer.SetBlock(x, y, z, stoneId);
        }
    }

    /// <summary>Creates an entity suitable for NBT save/load (registry items with invalid default state get a safe stack).</summary>
    public static Entity CreateForNbtRoundTrip(EntityType type, FakeWorldContext world)
    {
        if (type == EntityRegistry.Item)
        {
            return new EntityItem(world, 8.5, 65.0, 8.5, new ItemStack(Item.Stick, 1));
        }

        if (type == EntityRegistry.PrimedTnt)
        {
            return new EntityTntPrimed(world, 8.5, 66.0, 8.5);
        }

        if (type == EntityRegistry.Painting)
        {
            return new EntityPainting(world, 8, 65, 8, 2, "Kebab");
        }

        if (type == EntityRegistry.FallingSand)
        {
            return new EntityFallingSand(world, 8.5, 70.0, 8.5, Block.Sand.id);
        }

        if (type == EntityRegistry.Minecart)
        {
            return new EntityMinecart(world, 8.5, 65.0, 8.5, 0);
        }

        if (type == EntityRegistry.Boat)
        {
            return new EntityBoat(world, 8.5, 65.0, 8.5);
        }

        if (type == EntityRegistry.FishHook)
        {
            return new EntityFish(world, 8.5, 65.0, 8.5);
        }

        Entity entity = type.Create(world);
        entity.SetPositionAndAngles(8.5, 65.0, 8.5, 0f, 0f);
        return entity;
    }
}
