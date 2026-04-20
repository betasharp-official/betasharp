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
        SetPositionAndAnglesKeepPrevAngles(x, y, z, 0.0F, 0.0F);
        flashTimer = 2;
        renderSeed = Random.NextLong();
        flashCount = Random.NextInt(3) + 1;
        if (world.Difficulty >= 2 && world.ChunkHost.IsRegionLoaded(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z), 10))
        {
            int strikeX = MathHelper.Floor(x);
            int strikeY = MathHelper.Floor(y);
            int strikeZ = MathHelper.Floor(z);
            if (world.Reader.GetBlockId(strikeX, strikeY, strikeZ) == 0 && Block.Fire.CanPlaceAt(new CanPlaceAtContext(world, 0, strikeX, strikeY, strikeZ)))
            {
                world.Writer.SetBlock(strikeX, strikeY, strikeZ, Block.Fire.ID);
            }

            for (strikeX = 0; strikeX < 4; ++strikeX)
            {
                strikeY = MathHelper.Floor(x) + Random.NextInt(3) - 1;
                strikeZ = MathHelper.Floor(y) + Random.NextInt(3) - 1;
                int fireZ = MathHelper.Floor(z) + Random.NextInt(3) - 1;
                if (world.Reader.GetBlockId(strikeY, strikeZ, fireZ) == 0 && Block.Fire.CanPlaceAt(new CanPlaceAtContext(world, 0, strikeY, strikeZ, fireZ)))
                {
                    world.Writer.SetBlock(strikeY, strikeZ, fireZ, Block.Fire.ID);
                }
            }
        }

    }

    public override void Tick()
    {
        base.Tick();
        if (flashTimer == 2)
        {
            World.Broadcaster.PlaySoundAtPos(X, Y, Z, "ambient.weather.thunder", 10000.0F, 0.8F + Random.NextFloat() * 0.2F);
            World.Broadcaster.PlaySoundAtPos(X, Y, Z, "random.explode", 2.0F, 0.5F + Random.NextFloat() * 0.2F);
        }

        --flashTimer;
        if (flashTimer < 0)
        {
            if (flashCount == 0)
            {
                MarkDead();
            }
            else if (flashTimer < -Random.NextInt(10))
            {
                --flashCount;
                flashTimer = 1;
                renderSeed = Random.NextLong();
                if (World.ChunkHost.IsRegionLoaded(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z), 10))
                {
                    int floorX = MathHelper.Floor(X);
                    int floorY = MathHelper.Floor(Y);
                    int floorZ = MathHelper.Floor(Z);
                    if (World.Reader.GetBlockId(floorX, floorY, floorZ) == 0 && Block.Fire.CanPlaceAt(new CanPlaceAtContext(World, 0, floorX, floorY, floorZ)))
                    {
                        World.Writer.SetBlock(floorX, floorY, floorZ, Block.Fire.ID);
                    }
                }
            }
        }

        if (flashTimer >= 0)
        {
            double searchRadius = 3.0D;
            var entities = World.Entities.GetEntities(this, new Box(X - searchRadius, Y - searchRadius, Z - searchRadius, X + searchRadius, Y + 6.0D + searchRadius, Z + searchRadius));

            for (int i = 0; i < entities.Count; ++i)
            {
                Entity entity = entities[i];
                entity.OnStruckByLightning(this);
            }

            World.Environment.LightningTicksLeft = 2;
        }

    }


    public override void ReadNbt(NBTTagCompound nbt)
    {
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
    }

    public override bool ShouldRender(Vec3D cameraPos)
    {
        return flashTimer >= 0;
    }
}
