using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockWeb : Block
{
    public BlockWeb(int id, int texturePosition) : base(id, texturePosition, Material.Cobweb)
    {
    }

    public override void onEntityCollision(OnEntityCollisionEvent ctx) => ctx.Entity.Slowed = true;

    public override bool isOpaque() => false;

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override BlockRendererType getRenderType() => BlockRendererType.Reed;

    public override bool isFullCube() => false;

    public override int getDroppedItemId(int blockMeta) => Item.String.id;
}
