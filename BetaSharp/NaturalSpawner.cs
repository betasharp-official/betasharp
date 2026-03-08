using System.Xml;
using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.PathFinding;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Generation.Biomes;

namespace BetaSharp;

internal static class NaturalSpawner
{
    private const int SpawnMaxRadius = 8; // Expressed in chunks
    private const float SpawnMinRadius = 24.0F; // Expressed in blocks
    private const int SpawnCloseness = 6;

    private static readonly HashSet<ChunkPos> ChunksForSpawning = [];

    private static readonly Func<World, EntityLiving>[] Monsters =
    [
        w => new EntitySpider(w),
        w => new EntityZombie(w),
        w => new EntitySkeleton(w),
    ];

    private static BlockPos GetRandomSpawningPointInChunk(World world, PathFinder pathFinder, int centerX, int centerZ)
    {
        pathFinder.SetWorld(world.BlocksReader);
        int x = centerX + world.random.NextInt(16);
        int y = world.random.NextInt(128);
        int z = centerZ + world.random.NextInt(16);
        return new BlockPos(x, y, z);
    }

    internal static void DoSpawning(World world, PathFinder pathFinder, bool spawnHostile, bool spawnPeaceful)
    {
        pathFinder.SetWorld(world.BlocksReader);
        if (!spawnHostile && !spawnPeaceful) return;

        ChunksForSpawning.Clear();

        foreach (var p in world.Entities.Players)
        {
            int chunkX = MathHelper.Floor(p.x / 16.0D);
            int chunkZ = MathHelper.Floor(p.z / 16.0D);

            for (int x = -SpawnMaxRadius; x <= SpawnMaxRadius; ++x)
            {
                for (int z = -SpawnMaxRadius; z <= SpawnMaxRadius; ++z)
                {
                    ChunksForSpawning.Add(new ChunkPos(chunkX + x, chunkZ + z));
                }
            }
        }

        Vec3i worldSpawn = world.GetSpawnPos();
        foreach (var creatureKind in CreatureKind.Values)
        {
            if (((!creatureKind.Peaceful && spawnHostile) || (creatureKind.Peaceful && spawnPeaceful)) &&
                world.Entities.CountEntitiesOfType(creatureKind.EntityType) <=
                creatureKind.MobCap * ChunksForSpawning.Count / 256)
            {
                foreach (var chunk in ChunksForSpawning)
                {
                    Biome biome = world.GetBiomeSource().GetBiome(chunk);
                    var spawnSelector = biome.GetSpawnableList(creatureKind);
                    if (spawnSelector.Empty) break;
                    SpawnListEntry toSpawn = spawnSelector.GetNext(world.random);

                    BlockPos spawnPos = GetRandomSpawningPointInChunk(world, pathFinder, chunk.X * 16, chunk.Z * 16);
                    if (world.shouldSuffocate(spawnPos.x, spawnPos.y, spawnPos.z)) continue;
                    if (world.getMaterial(spawnPos.x, spawnPos.y, spawnPos.z) != creatureKind.SpawnMaterial) continue;

                    int spawnedCount = 0;
                    bool breakToNextChunk = false;

                    for (int i = 0; i < 3 && !breakToNextChunk; ++i)
                    {
                        int x = spawnPos.x;
                        int y = spawnPos.y;
                        int z = spawnPos.z;

                        for (int j = 0; j < 4 && !breakToNextChunk; ++j)
                        {
                            x += world.random.NextInt(SpawnCloseness) - world.random.NextInt(SpawnCloseness);
                            y += world.random.NextInt(1) - world.random.NextInt(1);
                            z += world.random.NextInt(SpawnCloseness) - world.random.NextInt(SpawnCloseness);
                            if (creatureKind.CanSpawnAtLocation(world, x, y, z))
                            {
                                Vec3D entityPos = new Vec3D(x + 0.5D, y, z + 0.5D);

                                if (world.Entities.GetClosestPlayer(entityPos.x, entityPos.y, entityPos.z, SpawnMinRadius) != null) continue;

                                if (entityPos.squareDistanceTo((Vec3D)worldSpawn) < SpawnMinRadius * SpawnMinRadius) continue;

                                EntityLiving entity = toSpawn.Factory(world);

                                entity.setPositionAndAnglesKeepPrevAngles(entityPos.x, entityPos.y, entityPos.z,
                                    world.random.NextFloat() * 360.0F, 0.0F);

                                if (entity.canSpawn())
                                {
                                    spawnedCount++;

                                    world.Entities.SpawnEntity(entity);

                                    entity.PostSpawn();
                                    if (spawnedCount >= entity.getMaxSpawnedInChunk())
                                    {
                                        breakToNextChunk = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    internal static bool SpawnMonstersAndWakePlayers(World world, PathFinder pathFinder, List<EntityPlayer> players)
    {
        pathFinder.SetWorld(world.BlocksReader);
        bool monstersSpawned = false;
        foreach (var player in players)
        {
            for (int i = 0; i < 20; ++i)
            {
                int spawnX = MathHelper.Floor(player.x) + world.random.NextInt(32) - world.random.NextInt(32);
                int spawnZ = MathHelper.Floor(player.z) + world.random.NextInt(32) - world.random.NextInt(32);
                int spawnY = MathHelper.Floor(player.y) + world.random.NextInt(16) - world.random.NextInt(16);
                if (spawnY < 1)
                {
                    spawnY = 1;
                }
                else if (spawnY > 128)
                {
                    spawnY = 128;
                }

                int r = world.random.NextInt(Monsters.Length);

                int newSpawnY;
                for (newSpawnY = spawnY; newSpawnY > 2; --newSpawnY)
                {
                    if (world.shouldSuffocate(spawnX, newSpawnY - 1, spawnZ)) break;
                }

                while (!CreatureKind.Monster.CanSpawnAtLocation(world, spawnX, newSpawnY, spawnZ) &&
                       newSpawnY < spawnY + 16 && newSpawnY < 128)
                {
                    ++newSpawnY;
                }

                if (newSpawnY < spawnY + 16 && newSpawnY < 128)
                {
                    EntityLiving entity = Monsters[r](world);

                    entity.setPositionAndAnglesKeepPrevAngles(spawnX + 0.5D, spawnY, spawnZ + 0.5D,
                        world.random.NextFloat() * 360.0F, 0.0F);
                    if (entity.canSpawn())
                    {
                        var pathEntity = pathFinder.CreateEntityPathTo(entity, player, 32.0F);
                        if (pathEntity != null && pathEntity.PathLength > 1)
                        {
                            PathPoint pathPoint = pathEntity.GetFinalPoint();
                            if (Math.Abs(pathPoint.X - player.x) < 1.5D && Math.Abs(pathPoint.Z - player.z) < 1.5D &&
                                Math.Abs(pathPoint.Y - player.y) < 1.5D)
                            {
                                Vec3i wakeUpPos =
                                    BlockBed.findWakeUpPosition(world, MathHelper.Floor(player.x),
                                        MathHelper.Floor(player.y), MathHelper.Floor(player.z), 1) ??
                                    new Vec3i(spawnX, newSpawnY + 1, spawnZ);

                                entity.setPositionAndAnglesKeepPrevAngles((double)((float)wakeUpPos.X + 0.5F),
                                    (double)wakeUpPos.Y, (double)((float)wakeUpPos.Z + 0.5F), 0.0F, 0.0F);
                                world.Entities.SpawnEntity(entity);
                                entity.PostSpawn();
                                player.wakeUp(true, false, false);
                                entity.playLivingSound();
                                monstersSpawned = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        return monstersSpawned;
    }
}
