using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockSoulSand : Block
{
    public BlockSoulSand(int id, int textureId) : base(id, textureId, Material.Sand)
    {
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        float height = 2.0F / 16.0F;
        return new Box(x, y, z, x + 1, y + 1 - height, z + 1);
    }

    public override void onEntityCollision(OnEntityCollisionEvent @event)
    {
        @event.Entity.velocityX *= 0.4;
        @event.Entity.velocityZ *= 0.4;
    }
}
