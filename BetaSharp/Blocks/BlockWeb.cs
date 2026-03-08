using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockWeb : Block
{
    public BlockWeb(int id, int texturePosition) : base(id, texturePosition, Material.Cobweb)
    {
    }

    public override void onEntityCollision(World world, int x, int y, int z, Entity entity) => entity.slowed = true;

    public override bool isOpaque() => false;

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override BlockRendererType getRenderType() => BlockRendererType.Reed;

    public override bool isFullCube() => false;

    public override int getDroppedItemId(int blockMeta) => Item.String.id;
}
