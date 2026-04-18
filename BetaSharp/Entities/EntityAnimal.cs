using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityAnimal(IWorldContext world) : EntityCreature(world), SpawnableEntity
{
    protected override float GetBlockPathWeight(int x, int y, int z) => World.Reader.GetBlockId(x, y - 1, z) == Block.GrassBlock.id ? 10.0F : World.Lighting.GetLuminance(x, y, z) - 0.5F;

    public override bool CanSpawn()
    {
        int x = MathHelper.Floor(X);
        int y = MathHelper.Floor(BoundingBox.MinY);
        int z = MathHelper.Floor(Z);
        return World.Reader.GetBlockId(x, y - 1, z) == Block.GrassBlock.id && World.Reader.GetBrightness(x, y, z) > 8 && base.CanSpawn();
    }

    protected override int TalkInterval => 120;
}
