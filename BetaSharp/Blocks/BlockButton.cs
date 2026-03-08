using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockButton : Block
{
    public BlockButton(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => setTickRandomly(true);

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override int getTickRate() => 20;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    private bool IsValidPlacementSide(IBlockReader read, int x, int y, int z, int side = 0)
    {
        if (side == 2)
        {
            return side == 2 && read.ShouldSuffocate(x, y, z + 1) ? true : side == 3 && read.ShouldSuffocate(x, y, z - 1) ? true : side == 4 && read.ShouldSuffocate(x + 1, y, z) ? true : side == 5 && read.ShouldSuffocate(x - 1, y, z);
        }

        return read.ShouldSuffocate(x - 1, y, z) ? true : read.ShouldSuffocate(x + 1, y, z) ? true : read.ShouldSuffocate(x, y, z - 1) ? true : read.ShouldSuffocate(x, y, z + 1);
    }

    public override bool canPlaceAt(OnPlacedEvt ctx) => IsValidPlacementSide(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, ctx.Side);

    public override void onPlaced(OnPlacedEvt ctx)
    {
        int facing = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int pressedBit = facing & 8;
        facing &= 7;
        if (ctx.Direction == 2 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1))
        {
            facing = 4;
        }
        else if (ctx.Direction == 3 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1))
        {
            facing = 3;
        }
        else if (ctx.Direction == 4 && ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z))
        {
            facing = 2;
        }
        else if (ctx.Direction == 5 && ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z))
        {
            facing = 1;
        }
        else
        {
            facing = getPlacementSide(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z);
        }

        ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, facing + pressedBit);
    }

    private int getPlacementSide(IBlockReader world, int x, int y, int z) =>
        world.ShouldSuffocate(x - 1, y, z) ? 1 : world.ShouldSuffocate(x + 1, y, z) ? 2 : world.ShouldSuffocate(x, y, z - 1) ? 3 : world.ShouldSuffocate(x, y, z + 1) ? 4 : 1;

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (breakIfCannotPlaceAt(ctx))
        {
            int facing = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) & 7;
            bool shouldBreak = false;
            if (!ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z) && facing == 1)
            {
                shouldBreak = true;
            }

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) && facing == 2)
            {
                shouldBreak = true;
            }

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) && facing == 3)
            {
                shouldBreak = true;
            }

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1) && facing == 4)
            {
                shouldBreak = true;
            }

            if (shouldBreak)
            {
                dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z));
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
    }

    private bool breakIfCannotPlaceAt(OnTickEvt ctx)
    {
        if (!IsValidPlacementSide(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z))
        {
            dropStacks(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            return false;
        }

        return true;
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        int meta = iBlockReader.GetBlockMeta(x, y, z);
        int facing = meta & 7;
        bool isPressed = (meta & 8) > 0;
        float minY = 6.0F / 16.0F;
        float maxY = 10.0F / 16.0F;
        float halfWidth = 3.0F / 16.0F;
        float thickness = 2.0F / 16.0F;
        if (isPressed)
        {
            thickness = 1.0F / 16.0F;
        }

        if (facing == 1)
        {
            setBoundingBox(0.0F, minY, 0.5F - halfWidth, thickness, maxY, 0.5F + halfWidth);
        }
        else if (facing == 2)
        {
            setBoundingBox(1.0F - thickness, minY, 0.5F - halfWidth, 1.0F, maxY, 0.5F + halfWidth);
        }
        else if (facing == 3)
        {
            setBoundingBox(0.5F - halfWidth, minY, 0.0F, 0.5F + halfWidth, maxY, thickness);
        }
        else if (facing == 4)
        {
            setBoundingBox(0.5F - halfWidth, minY, 1.0F - thickness, 0.5F + halfWidth, maxY, 1.0F);
        }
    }


    private bool updateState(IBlockReader reader, IBlockWrite writer, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        int meta = reader.GetBlockMeta(x, y, z);
        int facing = meta & 7;
        int pressToggle = 8 - (meta & 8);
        if (pressToggle == 0)
        {
            return true;
        }

        writer.SetBlockMeta(x, y, z, facing + pressToggle);
        writer.SetBlocksDirty(x, y, z, x, y, z);
        broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.5D, z + 0.5D, "random.click", 0.3F, 0.6F);
        broadcaster.NotifyNeighbors(x, y, z, id);
        if (facing == 1)
        {
            broadcaster.NotifyNeighbors(x - 1, y, z, id);
        }
        else if (facing == 2)
        {
            broadcaster.NotifyNeighbors(x + 1, y, z, id);
        }
        else if (facing == 3)
        {
            broadcaster.NotifyNeighbors(x, y, z - 1, id);
        }
        else if (facing == 4)
        {
            broadcaster.NotifyNeighbors(x, y, z + 1, id);
        }
        else
        {
            broadcaster.NotifyNeighbors(x, y - 1, z, id);
        }

        writer.ScheduleBlockUpdate(x, y, z, id, getTickRate());
        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvt ctx) => updateState(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z);

    public override bool onUse(OnUseEvt ctx) => updateState(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z);

    public override void onBreak(OnBreakEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if ((meta & 8) > 0)
        {
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
            int facing = meta & 7;
            if (facing == 1)
            {
                ctx.Broadcaster.NotifyNeighbors(ctx.X - 1, ctx.Y, ctx.Z, id);
            }
            else if (facing == 2)
            {
                ctx.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, id);
            }
            else if (facing == 3)
            {
                ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, id);
            }
            else if (facing == 4)
            {
                ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, id);
            }
            else
            {
                ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
            }
        }

        base.onBreak(ctx);
    }

    public override bool isPoweringSide(IBlockReader reader, int x, int y, int z, int side) => (reader.GetBlockMeta(x, y, z) & 8) > 0;

    public override bool isStrongPoweringSide(IBlockReader read, int x, int y, int z, int side)
    {
        int meta = read.GetBlockMeta(x, y, z);
        if ((meta & 8) == 0)
        {
            return false;
        }

        int facing = meta & 7;
        return facing == 5 && side == 1 ? true : facing == 4 && side == 2 ? true : facing == 3 && side == 3 ? true : facing == 2 && side == 4 ? true : facing == 1 && side == 5;
    }

    public override bool canEmitRedstonePower() => true;

    public override void onTick(OnTickEvt ctx)
    {
        if (!ctx.IsRemote)
        {
            int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            if ((meta & 8) != 0)
            {
                ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta & 7);
                ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
                int facing = meta & 7;
                if (facing == 1)
                {
                    ctx.Broadcaster.NotifyNeighbors(ctx.X - 1, ctx.Y, ctx.Z, id);
                }
                else if (facing == 2)
                {
                    ctx.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, id);
                }
                else if (facing == 3)
                {
                    ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, id);
                }
                else if (facing == 4)
                {
                    ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, id);
                }
                else
                {
                    ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
                }

                ctx.Broadcaster.PlaySoundAtPos(ctx.X + 0.5D, ctx.Y + 0.5D, ctx.Z + 0.5D, "random.click", 0.3F, 0.5F);
                ctx.WorldWrite.SetBlocksDirty(ctx.X, ctx.Y, ctx.Z);
            }
        }
    }

    public override void setupRenderBoundingBox()
    {
        float halfWidth = 3.0F / 16.0F;
        float halfHeight = 2.0F / 16.0F;
        float halfDepth = 2.0F / 16.0F;
        setBoundingBox(0.5F - halfWidth, 0.5F - halfHeight, 0.5F - halfDepth, 0.5F + halfWidth, 0.5F + halfHeight, 0.5F + halfDepth);
    }
}
