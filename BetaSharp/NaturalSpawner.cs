using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.PathFinding;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Generation.Biomes;

namespace BetaSharp;

internal static class NaturalSpawner
{
    private const int SpawnMaxRadius = 8; // Expressed in chunks
    private const float SpawnMinRadius = 24.0F; // Expressed in blocks
    private const int SpawnCloseness = 6;

    private static readonly HashSet<ChunkPos> ChunksForSpawning = [];

    private static readonly Func<IWorldContext, EntityLiving>[] Monsters =
    [
        w => new EntitySpider(w),
        w => new EntityZombie(w),
        w => new EntitySkeleton(w),
    ];

    private static BlockPos GetRandomSpawningPointInChunk(IWorldContext world, PathFinder pathFinder, int centerX, int centerZ)
    {
        pathFinder.SetWorld(world.Reader);
        int x = centerX + world.Random.NextInt(16);
        int y = world.Random.NextInt(128);
        int z = centerZ + world.Random.NextInt(16);
        return new BlockPos(x, y, z);
    }

    internal static void DoSpawning(IWorldContext world, PathFinder pathFinder, bool spawnHostile, bool spawnPeaceful)
    {
        pathFinder.SetWorld(world.Reader);
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

        Vec3i worldSpawn = world.Properties.GetSpawnPos();
        foreach (var creatureKind in CreatureKind.Values)
        {
            if (((!creatureKind.Peaceful && spawnHostile) || (creatureKind.Peaceful && spawnPeaceful)) &&
                world.Entities.CountEntitiesOfType(creatureKind.EntityType) <=
                creatureKind.MobCap * ChunksForSpawning.Count / 256)
            {
                foreach (var chunk in ChunksForSpawning)
                {
                    Biome biome = world.Dimension.BiomeSource.GetBiome(chunk);
                    var spawnSelector = biome.GetSpawnableList(creatureKind);
                    if (spawnSelector.Empty) break;
                    SpawnListEntry toSpawn = spawnSelector.GetNext(world.Random);

                    BlockPos spawnPos = GetRandomSpawningPointInChunk(world, pathFinder, chunk.X * 16, chunk.Z * 16);
                    if (world.Reader.ShouldSuffocate(spawnPos.x, spawnPos.y, spawnPos.z)) continue;
                    if (world.Reader.GetMaterial(spawnPos.x, spawnPos.y, spawnPos.z) != creatureKind.SpawnMaterial) continue;

                    int spawnedCount = 0;
                    bool breakToNextChunk = false;

                    for (int i = 0; i < 3 && !breakToNextChunk; ++i)
                    {
                        int x = spawnPos.x;
                        int y = spawnPos.y;
                        int z = spawnPos.z;

                        for (int j = 0; j < 4 && !breakToNextChunk; ++j)
                        {
                            x += world.Random.NextInt(SpawnCloseness) - world.Random.NextInt(SpawnCloseness);
                            y += world.Random.NextInt(1) - world.Random.NextInt(1);
                            z += world.Random.NextInt(SpawnCloseness) - world.Random.NextInt(SpawnCloseness);
                            if (creatureKind.CanSpawnAtLocation(world.Reader, x, y, z))
                            {
                                Vec3D entityPos = new Vec3D(x + 0.5D, y, z + 0.5D);

                                if (world.Entities.GetClosestPlayer(entityPos.x, entityPos.y, entityPos.z, SpawnMinRadius) != null) continue;

                                if (entityPos.squareDistanceTo((Vec3D)worldSpawn) < SpawnMinRadius * SpawnMinRadius) continue;

                                EntityLiving entity = toSpawn.Factory(world);

                                entity.setPositionAndAnglesKeepPrevAngles(entityPos.x, entityPos.y, entityPos.z,
                                    world.Random.NextFloat() * 360.0F, 0.0F);

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

    internal static bool SpawnMonstersAndWakePlayers(IWorldContext world, List<EntityPlayer> players)
    {
        world.Pathing.SetWorld(world.Reader);
        bool monstersSpawned = false;
        foreach (var player in players)
        {
            for (int i = 0; i < 20; ++i)
            {
                int spawnX = MathHelper.Floor(player.x) + world.Random.NextInt(32) - world.Random.NextInt(32);
                int spawnZ = MathHelper.Floor(player.z) + world.Random.NextInt(32) - world.Random.NextInt(32);
                int spawnY = MathHelper.Floor(player.y) + world.Random.NextInt(16) - world.Random.NextInt(16);
                if (spawnY < 1)
                {
                    spawnY = 1;
                }
                else if (spawnY > 128)
                {
                    spawnY = 128;
                }

                int r = world.Random.NextInt(Monsters.Length);

                int newSpawnY;
                for (newSpawnY = spawnY; newSpawnY > 2; --newSpawnY)
                {
                    if (world.Reader.ShouldSuffocate(spawnX, newSpawnY - 1, spawnZ)) break;
                }

                while (!CreatureKind.Monster.CanSpawnAtLocation(world.Reader, spawnX, newSpawnY, spawnZ) &&
                       newSpawnY < spawnY + 16 && newSpawnY < 128)
                {
                    ++newSpawnY;
                }

                if (newSpawnY < spawnY + 16 && newSpawnY < 128)
                {
                    EntityLiving entity = Monsters[r](world);

                    // Feet must be on the validated air column (newSpawnY), not random spawnY — spawnY
                    // can be inside stone and collision resolution rockets mobs to the surface.
                    entity.setPositionAndAnglesKeepPrevAngles(spawnX + 0.5D, newSpawnY, spawnZ + 0.5D,
                        world.Random.NextFloat() * 360.0F, 0.0F);
                    if (entity.canSpawn())
                    {
                        var pathEntity = world.Pathing.findPath(entity, player, 32.0F);
                        if (pathEntity != null && pathEntity.PathLength > 1)
                        {
                            PathPoint? pathPoint = pathEntity.GetFinalPoint();
                            if (Math.Abs(pathPoint.X - player.x) < 1.5D && Math.Abs(pathPoint.Z - player.z) < 1.5D &&
                                Math.Abs(pathPoint.Y - player.y) < 1.5D)
                            {
                                Vec3i wakeUpPos =
                                    BlockBed.findWakeUpPosition(world.Reader, MathHelper.Floor(player.x),
                                        MathHelper.Floor(player.y), MathHelper.Floor(player.z), 1) ??
                                    new Vec3i(spawnX, newSpawnY + 1, spawnZ);

                                entity.setPositionAndAnglesKeepPrevAngles(wakeUpPos.X + 0.5F,wakeUpPos.Y, wakeUpPos.Z + 0.5F, 0.0F, 0.0F);
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
