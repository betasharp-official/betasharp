using BetaSharp.Blocks;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityLightningBolt : EntityWeatherEffect
{
    private int _flashCount;

    private int _flashTimer;
    public long RenderSeed;

    public EntityLightningBolt(IWorldContext world, double x = 0, double y = 0, double z = 0) : base(world)
    {
        SetPositionAndAnglesKeepPrevAngles(x, y, z, 0.0F, 0.0F);
        _flashTimer = 2;
        RenderSeed = Random.NextLong();
        _flashCount = Random.NextInt(3) + 1;
        if (world.Difficulty < 2 || !world.ChunkHost.IsRegionLoaded(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z), 10))
        {
            return;
        }

        int strikeX = MathHelper.Floor(x);
        int strikeY = MathHelper.Floor(y);
        int strikeZ = MathHelper.Floor(z);
        if (world.Reader.GetBlockId(strikeX, strikeY, strikeZ) == 0 && Block.Fire.canPlaceAt(new CanPlaceAtContext(world, 0, strikeX, strikeY, strikeZ)))
        {
            world.Writer.SetBlock(strikeX, strikeY, strikeZ, Block.Fire.id);
        }

        for (strikeX = 0; strikeX < 4; ++strikeX)
        {
            strikeY = MathHelper.Floor(x) + Random.NextInt(3) - 1;
            strikeZ = MathHelper.Floor(y) + Random.NextInt(3) - 1;
            int fireZ = MathHelper.Floor(z) + Random.NextInt(3) - 1;
            if (world.Reader.GetBlockId(strikeY, strikeZ, fireZ) == 0 && Block.Fire.canPlaceAt(new CanPlaceAtContext(world, 0, strikeY, strikeZ, fireZ)))
            {
                world.Writer.SetBlock(strikeY, strikeZ, fireZ, Block.Fire.id);
            }
        }
    }

    public override EntityType Type => EntityRegistry.LightningBolt;

    public override void Tick()
    {
        base.Tick();
        if (_flashTimer == 2)
        {
            World.Broadcaster.PlaySoundAtPos(X, Y, Z, "ambient.weather.thunder", 10000.0F, 0.8F + Random.NextFloat() * 0.2F);
            World.Broadcaster.PlaySoundAtPos(X, Y, Z, "random.explode", 2.0F, 0.5F + Random.NextFloat() * 0.2F);
        }

        --_flashTimer;
        if (_flashTimer < 0)
        {
            if (_flashCount == 0)
            {
                MarkDead();
            }
            else if (_flashTimer < -Random.NextInt(10))
            {
                --_flashCount;
                _flashTimer = 1;
                RenderSeed = Random.NextLong();
                if (World.ChunkHost.IsRegionLoaded(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z), 10))
                {
                    int floorX = MathHelper.Floor(X);
                    int floorY = MathHelper.Floor(Y);
                    int floorZ = MathHelper.Floor(Z);
                    if (World.Reader.GetBlockId(floorX, floorY, floorZ) == 0 && Block.Fire.canPlaceAt(new CanPlaceAtContext(World, 0, floorX, floorY, floorZ)))
                    {
                        World.Writer.SetBlock(floorX, floorY, floorZ, Block.Fire.id);
                    }
                }
            }
        }

        if (_flashTimer < 0) return;

        const double searchRadius = 3.0D;
        List<Entity> entities = World.Entities.GetEntities(this, new Box(X - searchRadius, Y - searchRadius, Z - searchRadius, X + searchRadius, Y + 6.0D + searchRadius, Z + searchRadius));

        foreach (Entity entity in entities)
        {
            entity.OnStruckByLightning(this);
        }

        World.Environment.LightningTicksLeft = 2;
    }


    protected override void ReadNbt(NBTTagCompound nbt)
    {
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
    }

    public override bool ShouldRender(Vec3D cameraPos) => _flashTimer >= 0;
}
