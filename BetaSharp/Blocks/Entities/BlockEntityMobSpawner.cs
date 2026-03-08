using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks.Entities;

public class BlockEntityMobSpawner : BlockEntity
{
    private readonly ILogger<BlockEntityMobSpawner> _logger = Log.Instance.For<BlockEntityMobSpawner>();
    private string _spawnedEntityId = "Pig";

    public BlockEntityMobSpawner() => SpawnDelay = 20;
    public int SpawnDelay { get; set; } = -1;
    public double Rotation { get; set; }
    public double LastRotation { get; set; }

    public string GetSpawnedEntityId() => _spawnedEntityId;

    public void SetSpawnedEntityId(string spawnedEntityId) => _spawnedEntityId = spawnedEntityId;

    public bool IsPlayerInRange() => World.getClosestPlayer(X + 0.5D, Y + 0.5D, Z + 0.5D, 16.0D) != null;

    public override void tick()
    {
        LastRotation = Rotation;
        if (IsPlayerInRange())
        {
            double particleX = X + World.random.NextFloat();
            double particleY = Y + World.random.NextFloat();
            double particleZ = Z + World.random.NextFloat();
            World.addParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            World.addParticle("flame", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);

            for (Rotation += 1000.0F / (SpawnDelay + 200.0F); Rotation > 360.0D; LastRotation -= 360.0D)
            {
                Rotation -= 360.0D;
            }

            if (!World.isRemote)
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
                    EntityLiving entityLiving = (EntityLiving)EntityRegistry.Create(_spawnedEntityId, World);
                    if (entityLiving == null)
                    {
                        return;
                    }

                    int count = World.CollectEntitiesOfType<EntityLiving>(new Box(X, Y, Z, X + 1, Y + 1, Z + 1).Expand(8.0D, 4.0D, 8.0D)).Where(e => e.GetType() == entityLiving.GetType()).Count();
                    if (count >= 6)
                    {
                        ResetDelay();
                        return;
                    }

                    if (entityLiving != null)
                    {
                        double posX = X + (World.random.NextDouble() - World.random.NextDouble()) * 4.0D;
                        double posY = Y + World.random.NextInt(3) - 1;
                        double posZ = Z + (World.random.NextDouble() - World.random.NextDouble()) * 4.0D;
                        entityLiving.setPositionAndAnglesKeepPrevAngles(posX, posY, posZ, World.random.NextFloat() * 360.0F, 0.0F);
                        if (entityLiving.canSpawn())
                        {
                            World.SpawnEntity(entityLiving);

                            for (int particleIndex = 0; particleIndex < 20; ++particleIndex)
                            {
                                particleX = X + 0.5D + (World.random.NextFloat() - 0.5D) * 2.0D;
                                particleY = Y + 0.5D + (World.random.NextFloat() - 0.5D) * 2.0D;
                                particleZ = Z + 0.5D + (World.random.NextFloat() - 0.5D) * 2.0D;
                                World.addParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                                World.addParticle("flame", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                            }

                            entityLiving.animateSpawn();
                            ResetDelay();
                        }
                    }
                }
            }

            base.tick();
        }
    }

    private void ResetDelay()
    {
        SpawnDelay = 200 + World.random.NextInt(600);
        _logger.LogInformation("Spawn Delay: " + SpawnDelay);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        _spawnedEntityId = nbt.GetString("EntityId");
        SpawnDelay = nbt.GetShort("Delay");
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetString("EntityId", _spawnedEntityId);
        nbt.SetShort("Delay", (short)SpawnDelay);
    }
}
