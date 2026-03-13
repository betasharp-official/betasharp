using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockCake : Block
{
    public BlockCake(int id, int textureId) : base(id, textureId, Material.Cake) => setTickRandomly(true);

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        int slicesEaten = iBlockReader.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        setBoundingBox(minX, 0.0F, edgeInset, 1.0F - edgeInset, height, 1.0F - edgeInset);
    }

    public override void setupRenderBoundingBox()
    {
        float edgeInset = 1.0F / 16.0F;
        float height = 0.5F;
        setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, height, 1.0F - edgeInset);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        int slicesEaten = world.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box((double)((float)x + minX), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)y + height - edgeInset), (double)((float)(z + 1) - edgeInset));
    }

    public override Box getBoundingBox(IBlockReader world, int x, int y, int z)
    {
        int slicesEaten = world.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box((double)((float)x + minX), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)y + height), (double)((float)(z + 1) - edgeInset));
    }

    public override int getTexture(int side, int meta)
    {
        return side == 1 ? textureId : (side == 0 ? textureId + 3 : (meta > 0 && side == 4 ? textureId + 2 : textureId + 1));
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId : (side == 0 ? textureId + 3 : textureId + 1);
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool onUse(OnUseEvt evt)
    {
        if (evt.Player.health < 20)
        {
            evt.Player.heal(3);
            int slicesEaten = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z) + 1;
            if (slicesEaten >= 6)
            {
                evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
            }
            else
            {
                evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, slicesEaten);
                evt.Level.Broadcaster.SetBlocksDirty(evt.X, evt.Y, evt.Z);
            }
        }

        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvt evt)
    {
        if (evt.Player.health >= 20)
        {
            return;
        }

        evt.Player.heal(3);
        int slicesEaten = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z) + 1;
        if (slicesEaten >= 6)
        {
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
        }
        else
        {
            evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, slicesEaten);
            evt.Level.Broadcaster.SetBlocksDirty(evt.X, evt.Y, evt.Z);
        }
    }

    public override bool canPlaceAt(CanPlaceAtCtx evt)
    {
        return !base.canPlaceAt(evt) ? false : canGrow(evt.Level.Reader, evt.X, evt.Y, evt.Z);
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        if (canGrow(evt.Level.Reader, evt.X, evt.Y, evt.Z))
        {
            return;
        }

        dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
        evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
    }

    public override bool canGrow(OnTickEvt evt)
    {
        return canGrow(evt.Level.Reader, evt.X, evt.Y, evt.Z);
    }

    private static bool canGrow(IBlockReader world, int x, int y, int z)
    {
        return world.GetMaterial(x, y - 1, z).IsSolid;
    }

    public override int getDroppedItemCount()
    {
        return 0;
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return 0;
    }
}
