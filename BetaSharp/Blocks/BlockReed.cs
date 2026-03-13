using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockReed : Block
{
    public BlockReed(int id, int textureId) : base(id, Material.Plant)
    {
        this.textureId = textureId;
        float halfWidth = 6.0F / 16.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, 1.0F, 0.5F + halfWidth);
        setTickRandomly(true);
    }

    public override void onTick(OnTickEvt evt)
    {
        if (evt.Level.Reader.IsAir(evt.X, evt.Y + 1, evt.Z))
        {
            int heightBelow;
            for (heightBelow = 1; evt.Level.Reader.GetBlockId(evt.X, evt.Y - heightBelow, evt.Z) == id; ++heightBelow)
            {
            }

            if (heightBelow < 3)
            {
                int meta = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
                if (meta == 15)
                {
                    evt.Level.BlockWriter.SetBlock(evt.X, evt.Y + 1, evt.Z, id);
                    evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, 0);
                }
                else
                {
                    evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, meta + 1);
                }
            }
        }
    }

    public override bool canPlaceAt(CanPlaceAtCtx evt)
    {
        int blockBelowId = evt.Level.Reader.GetBlockId(evt.X, evt.Y - 1, evt.Z);
        return blockBelowId == id ? true :
            blockBelowId != GrassBlock.id && blockBelowId != Dirt.id ? false :
            evt.Level.Reader.GetMaterial(evt.X - 1, evt.Y - 1, evt.Z) == Material.Water ? true :
            evt.Level.Reader.GetMaterial(evt.X + 1, evt.Y - 1, evt.Z) == Material.Water ? true :
            evt.Level.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z - 1) == Material.Water ? true : evt.Level.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z + 1) == Material.Water;
    }

    public override void neighborUpdate(OnTickEvt evt) => breakIfCannotGrow(evt);

    protected void breakIfCannotGrow(OnTickEvt evt)
    {
        if (!canGrow(evt))
        {
            // TODO: Implement this
            dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
        }
    }

    public override bool canGrow(OnTickEvt evt) => canPlaceAt(new CanPlaceAtCtx(evt.Level, 0, evt.X, evt.Y, evt.Z));

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override int getDroppedItemId(int blockMeta) => Item.SugarCane.id;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Reed;
}
