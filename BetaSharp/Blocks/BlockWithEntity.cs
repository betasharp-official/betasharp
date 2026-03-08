using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public abstract class BlockWithEntity : Block
{
    protected BlockWithEntity(int id, Material material) : base(id, material) => BlocksWithEntity[id] = true;

    protected BlockWithEntity(int id, int textureId, Material material) : base(id, textureId, material) => BlocksWithEntity[id] = true;

    public override void onPlaced(World world, int x, int y, int z)
    {
        base.onPlaced(world, x, y, z);
        world.setBlockEntity(x, y, z, getBlockEntity());
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        base.onBreak(world, x, y, z);
        world.removeBlockEntity(x, y, z);
    }

    protected abstract BlockEntity getBlockEntity();
}
