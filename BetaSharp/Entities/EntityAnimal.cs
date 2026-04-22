using BetaSharp.Blocks;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityAnimal : EntityCreature, SpawnableEntity
{
    public EntityAnimal(IWorldContext world) : base(world)
    {
    }

    protected override float getBlockPathWeight(int x, int y, int z)
    {
        return World.Reader.GetBlockId(x, y - 1, z) == Block.GrassBlock.id ? 10.0F : World.Lighting.GetLuminance(x, y, z) - 0.5F;
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
    }

    public override bool canSpawn()
    {
        int x = MathHelper.Floor(base.X);
        int y = MathHelper.Floor(BoundingBox.MinY);
        int z = MathHelper.Floor(base.Z);
        return World.Reader.GetBlockId(x, y - 1, z) == Block.GrassBlock.id && World.Reader.GetBrightness(x, y, z) > 8 && base.canSpawn();
    }

    public override int getTalkInterval()
    {
        return 120;
    }
}
