using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockWeb : Block
{
    public BlockWeb(int id, int texturePosition) : base(id, texturePosition, Material.Cobweb)
    {
    }

    public override void onEntityCollision(World world, int x, int y, int z, Entity entity)
    {
        entity.slowed = true;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return null;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Reed;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Item.String.id;
    }
}
