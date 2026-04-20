using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks.Entities;

/// <summary>
///     Block entity for mob spawners, allowing them to spawn mobs and store the mob type and spawn delay.
/// </summary>
public class BlockEntityMobSpawner : BlockEntity
{
    private readonly ILogger<BlockEntityMobSpawner> _logger = Log.Instance.For<BlockEntityMobSpawner>();

    public BlockEntityMobSpawner() => SpawnDelay = 20;
    public override BlockEntityType Type => MobSpawner;

    public int SpawnDelay { get; set; } = -1;
    public string SpawnedEntityId { get; set; } = "Pig";

    public double Rotation { get; set; }
    public double LastRotation { get; set; }

    public bool IsPlayerInRange() => World.Entities.GetClosestPlayer(X + 0.5D, Y + 0.5D, Z + 0.5D, 16.0D) != null;

    public override void Tick(EntityManager entities)
    {
        LastRotation = Rotation;
        if (IsPlayerInRange())
        {
            double particleX = X + World.Random.NextFloat();
            double particleY = Y + World.Random.NextFloat();
            double particleZ = Z + World.Random.NextFloat();
            World.Broadcaster.AddParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            World.Broadcaster.AddParticle("flame", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);

            for (Rotation += 1000.0F / (SpawnDelay + 200.0F); Rotation > 360.0D; LastRotation -= 360.0D)
            {
                Rotation -= 360.0D;
            }

            if (!World.IsRemote)
            {
                if (SpawnDelay == -1)
                {
                    ResetDelay();
                }

                if (SpawnDelay > 0)
                {
                    --SpawnDelay;
                    return;
                }

                byte max = 4;

                for (int spawnAttempt = 0; spawnAttempt < max; ++spawnAttempt)
                {
                    EntityLiving? entityLiving = (EntityLiving?)EntityRegistry.Create(SpawnedEntityId, World);
                    if (entityLiving == null)
                    {
                        return;
                    }

                    int count = World.Entities
                        .CollectEntitiesOfType<EntityLiving>(new Box(X, Y, Z, X + 1, Y + 1, Z + 1)
                            .Expand(8.0D, 4.0D, 8.0D))
                        .Count(e => e.GetType() == entityLiving.GetType());
                    if (count >= 6)
                    {
                        ResetDelay();
                        return;
                    }

                    if (entityLiving != null)
                    {
                        double posX = X + (World.Random.NextDouble() - World.Random.NextDouble()) * 4.0D;
                        double posY = Y + World.Random.NextInt(3) - 1;
                        double posZ = Z + (World.Random.NextDouble() - World.Random.NextDouble()) * 4.0D;
                        entityLiving.SetPositionAndAnglesKeepPrevAngles(posX, posY, posZ, World.Random.NextFloat() * 360.0F, 0.0F);
                        if (entityLiving.canSpawn())
                        {
                            World.SpawnEntity(entityLiving);

                            for (int particleIndex = 0; particleIndex < 20; ++particleIndex)
                            {
                                particleX = X + 0.5D + (World.Random.NextFloat() - 0.5D) * 2.0D;
                                particleY = Y + 0.5D + (World.Random.NextFloat() - 0.5D) * 2.0D;
                                particleZ = Z + 0.5D + (World.Random.NextFloat() - 0.5D) * 2.0D;
                                World.Broadcaster.AddParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                                World.Broadcaster.AddParticle("flame", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                            }

                            entityLiving.animateSpawn();
                            ResetDelay();
                        }
                    }
                }
            }

            base.Tick(entities);
        }
    }

    private void ResetDelay()
    {
        SpawnDelay = 200 + World.Random.NextInt(600);
        _logger.LogInformation("Spawn Delay: " + SpawnDelay);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        SpawnedEntityId = nbt.GetString("EntityId");
        SpawnDelay = nbt.GetShort("Delay");
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetString("EntityId", SpawnedEntityId);
        nbt.SetShort("Delay", (short)SpawnDelay);
    }
}
