using BetaSharp.Blocks;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityLightningBolt : EntityWeatherEffect
{
    public override EntityType Type => EntityRegistry.LightningBolt;

    private int flashTimer;
    public long renderSeed;
    private int flashCount;

    public EntityLightningBolt(IWorldContext world) : this(world, 0, 0, 0) { }
    public EntityLightningBolt(IWorldContext world, double x, double y, double z) : base(world)
    {
        setPositionAndAnglesKeepPrevAngles(x, y, z, 0.0F, 0.0F);
        flashTimer = 2;
        renderSeed = random.NextLong();
        flashCount = random.NextInt(3) + 1;
        if (world.Difficulty >= 2 && world.ChunkHost.IsRegionLoaded(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z), 10))
        {
            int strikeX = MathHelper.Floor(x);
            int strikeY = MathHelper.Floor(y);
            int strikeZ = MathHelper.Floor(z);
            if (world.Reader.GetBlockId(strikeX, strikeY, strikeZ) == 0 && Block.Fire.canPlaceAt(new CanPlaceAtContext(world, 0, strikeX, strikeY, strikeZ)))
            {
                world.Writer.SetBlock(strikeX, strikeY, strikeZ, Block.Fire.id);
            }

            for (strikeX = 0; strikeX < 4; ++strikeX)
            {
                strikeY = MathHelper.Floor(x) + random.NextInt(3) - 1;
                strikeZ = MathHelper.Floor(y) + random.NextInt(3) - 1;
                int fireZ = MathHelper.Floor(z) + random.NextInt(3) - 1;
                if (world.Reader.GetBlockId(strikeY, strikeZ, fireZ) == 0 && Block.Fire.canPlaceAt(new CanPlaceAtContext(world, 0, strikeY, strikeZ, fireZ)))
                {
                    world.Writer.SetBlock(strikeY, strikeZ, fireZ, Block.Fire.id);
                }
            }
        }

    }

    public override void tick()
    {
        base.tick();
        if (flashTimer == 2)
        {
            world.Broadcaster.PlaySoundAtPos(x, y, z, "ambient.weather.thunder", 10000.0F, 0.8F + random.NextFloat() * 0.2F);
            world.Broadcaster.PlaySoundAtPos(x, y, z, "random.explode", 2.0F, 0.5F + random.NextFloat() * 0.2F);
        }

        --flashTimer;
        if (flashTimer < 0)
        {
            if (flashCount == 0)
            {
                markDead();
            }
            else if (flashTimer < -random.NextInt(10))
            {
                --flashCount;
                flashTimer = 1;
                renderSeed = random.NextLong();
                if (world.ChunkHost.IsRegionLoaded(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z), 10))
                {
                    int floorX = MathHelper.Floor(x);
                    int floorY = MathHelper.Floor(y);
                    int floorZ = MathHelper.Floor(z);
                    if (world.Reader.GetBlockId(floorX, floorY, floorZ) == 0 && Block.Fire.canPlaceAt(new CanPlaceAtContext(world, 0, floorX, floorY, floorZ)))
                    {
                        world.Writer.SetBlock(floorX, floorY, floorZ, Block.Fire.id);
                    }
                }
            }
        }

        if (flashTimer >= 0)
        {
            double searchRadius = 3.0D;
            var entities = world.Entities.GetEntities(this, new Box(x - searchRadius, y - searchRadius, z - searchRadius, x + searchRadius, y + 6.0D + searchRadius, z + searchRadius));

            for (int i = 0; i < entities.Count; ++i)
            {
                Entity entity = entities[i];
                entity.onStruckByLightning(this);
            }

            world.Environment.LightningTicksLeft = 2;
        }

    }


    public override void readNbt(NBTTagCompound nbt)
    {
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
    }

    public override bool shouldRender(Vec3D cameraPos)
    {
        return flashTimer >= 0;
    }
}
