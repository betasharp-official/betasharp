using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockDoor : Block
{
    public BlockDoor(int id, Material material) : base(id, material)
    {
        textureId = 97;
        if (material == Material.Metal)
        {
            ++textureId;
        }

        float halfWidth = 0.5F;
        float height = 1.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, height, 0.5F + halfWidth);
    }

    public override int getTexture(int side, int meta)
    {
        if (side != 0 && side != 1)
        {
            int facing = setOpen(meta);
            if ((facing == 0 || facing == 2) ^ (side <= 3))
            {
                return textureId;
            }

            int textureIndex = facing / 2 + ((side & 1) ^ facing);
            textureIndex += (meta & 4) / 4;
            int textureId = this.textureId - (meta & 8) * 2;
            if ((textureIndex & 1) != 0)
            {
                textureId = -textureId;
            }

            return textureId;
        }

        return textureId;
    }

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Door;

    public override Box getBoundingBox(IBlockReader world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, x, y, z);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getCollisionShape(world, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z) => rotate(setOpen(iBlockReader.GetBlockMeta(x, y, z)));

    public void rotate(int meta)
    {
        float thickness = 3.0F / 16.0F;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F, 1.0F);
        if (meta == 0)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
        }

        if (meta == 1)
        {
            setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
        }
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvt ctx) => updateDorState(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z);

    private bool updateDorState(IBlockReader reader, IBlockWrite writer, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        if (material == Material.Metal)
        {
            return true;
        }

        int meta = reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (reader.GetBlockId(x, y - 1, z) == id)
            {
                updateDorState(reader, writer, broadcaster, x, y - 1, z);
            }

            return true;
        }

        if (reader.GetBlockId(x, y + 1, z) == id)
        {
            writer.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
        }

        writer.SetBlockMeta(x, y, z, meta ^ 4);
        writer.SetBlocksDirty(x, y - 1, z, x, y, z);
        broadcaster.WorldEvent(1003, x, y, z, 0);
        return true;
    }


    public override bool onUse(OnUseEvt ctx) => updateDorState(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z);

    public void setOpen(IBlockReader reader, IBlockWrite writer, WorldEventBroadcaster broadcaster, int x, int y, int z, bool open)
    {
        int meta = reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (reader.GetBlockId(x, y - 1, z) == id)
            {
                setOpen(reader, writer, broadcaster, x, y - 1, z, open);
            }
        }
        else
        {
            bool isOpen = (reader.GetBlockMeta(x, y, z) & 4) > 0;
            if (isOpen != open)
            {
                if (reader.GetBlockId(x, y + 1, z) == id)
                {
                    writer.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
                }

                writer.SetBlockMeta(x, y, z, meta ^ 4);
                writer.SetBlocksDirty(x, y - 1, z, x, y, z);
                broadcaster.WorldEvent(1003, x, y, z, 0);
            }
        }
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if ((meta & 8) != 0)
        {
            if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z) != id)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }

            if (ctx.BlockId > 0 && Blocks[ctx.BlockId].canEmitRedstonePower())
            {
                neighborUpdate(ctx);
            }
        }
        else
        {
            bool wasBroken = false;
            if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y + 1, ctx.Z) != id)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                wasBroken = true;
            }

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z))
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                wasBroken = true;
                if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y + 1, ctx.Z) == id)
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y + 1, ctx.Z, 0);
                }
            }

            if (wasBroken)
            {
                if (!ctx.IsRemote)
                {
                    // TODO: Implement this
                    // dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, meta);
                }
            }
            else if (ctx.BlockId > 0 && Blocks[ctx.BlockId].canEmitRedstonePower())
            {
                bool isPowered = ctx.Redstone.IsPowered(ctx.X, ctx.Y, ctx.Z) || ctx.Redstone.IsPowered(ctx.X, ctx.Y + 1, ctx.Z);
                setOpen(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z, isPowered);
            }
        }
    }

    public override int getDroppedItemId(int blockMeta) => (blockMeta & 8) != 0 ? 0 : material == Material.Metal ? Item.IronDoor.id : Item.WoodenDoor.id;

    public override HitResult raycast(IBlockReader world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, x, y, z);
        return base.raycast(world, x, y, z, startPos, endPos);
    }

    public int setOpen(int meta) => (meta & 4) == 0 ? (meta - 1) & 3 : meta & 3;

    public override bool canPlaceAt(CanPlaceAtCtx ctx) => ctx.Y >= 127 ? false : ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && base.canPlaceAt(ctx) && base.canPlaceAt(ctx);

    public static bool isOpen(int meta) => (meta & 4) != 0;

    public override int getPistonBehavior() => 1;
}
