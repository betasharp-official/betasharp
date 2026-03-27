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
        return world.Reader.GetBlockId(x, y - 1, z) == Block.GrassBlock.id ? 10.0F : world.Lighting.GetLuminance(x, y, z) - 0.5F;
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
    }

    public override bool canSpawn()
    {
        int x = MathHelper.Floor(base.x);
        int y = MathHelper.Floor(boundingBox.MinY);
        int z = MathHelper.Floor(base.z);
        return world.Reader.GetBlockId(x, y - 1, z) == Block.GrassBlock.id && world.Reader.GetBrightness(x, y, z) > 8 && base.canSpawn();
    }

    public override int getTalkInterval()
    {
        return 120;
    }
}
