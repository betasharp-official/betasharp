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

    public override void OnEntityCollision(OnEntityCollisionEvent ctx) => ctx.Entity.Slowed = true;

    public override bool IsOpaque() => false;

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override BlockRendererType GetRenderType() => BlockRendererType.Reed;

    public override bool IsFullCube() => false;

    public override int GetDroppedItemId(int blockMeta) => Item.String.id;
}
