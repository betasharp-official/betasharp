using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonBase : Block
{
    private readonly bool sticky;
    private bool deaf;

    public BlockPistonBase(int id, int textureId, bool sticky) : base(id, textureId, Material.Piston)
    {
        this.sticky = sticky;
        setSoundGroup(s_soundStoneFootstep);
        setHardness(0.5F);
    }

    public int getTopTexture() => sticky ? 106 : 107;

    public override int getTexture(int side) =>
        side switch
        {
            1 => getTopTexture(),
            0 => 109,
            _ => 108
        };

    public override int getTexture(int side, int meta)
    {
        int var3 = getFacing(meta);
        return var3 > 5
            ? textureId
            : side == var3
                ? !isExtended(meta) && BoundingBox.MinX <= 0.0D && BoundingBox.MinY <= 0.0D && BoundingBox.MinZ <= 0.0D && BoundingBox.MaxX >= 1.0D && BoundingBox.MaxY >= 1.0D && BoundingBox.MaxZ >= 1.0D ? textureId : 110
                : side == PistonConstants.field_31057_a[var3]
                    ? 109
                    : 108;
    }

    public override BlockRendererType getRenderType() => BlockRendererType.PistonBase;

    public override bool isOpaque() => false;

    public override bool onUse(OnUseEvt ctx) => false;

    public override void onPlaced(OnPlacedEvt ctx)
    {
        int facing = getFacingForPlacement(ctx.Level, ctx.X, ctx.Y, ctx.Z, (EntityPlayer)ctx.Placer);
        ctx.Level.BlockWriter.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, facing);
        
        if (!ctx.Level.IsRemote)
        {
            checkExtended(ctx.Level, ctx.X, ctx.Y, ctx.Z);
        }
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (!ctx.Level.IsRemote && ctx.Level.Entities.GetBlockEntity(ctx.X, ctx.Y, ctx.Z) == null)
        {
            checkExtended(ctx.Level, ctx.X, ctx.Y, ctx.Z);
        }
    }

    private void checkExtended(IBlockWorldContext ctx, int x, int y, int z)
    {
        int meta = ctx.BlocksReader.GetBlockMeta(x, y, z);
        int facing = getFacing(meta);
        bool needsExtension = shouldExtend(ctx, x, y, z, facing);
        
        if (meta != 7)
        {
            if (needsExtension && !isExtended(meta))
            {
                if (canExtend(ctx, x, y, z, facing))
                {
                    ctx.BlockWriter.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing | 8);
                    // Routed directly into the block action system
                    onBlockAction(new OnBlockActionEvt(ctx, 0, facing, x, y, z));
                }
            }
            else if (!needsExtension && isExtended(meta))
            {
                ctx.BlockWriter.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing);
                // Routed directly into the block action system
                onBlockAction(new OnBlockActionEvt(ctx, 1, facing, x, y, z));
            }
        }
    }

    private bool shouldExtend(IBlockWorldContext ctx, int x, int y, int z, int facing) =>
        facing != 0 && ctx.Redstone.IsPoweringSide(x, y - 1, z, 0)
            ? true
            : facing != 1 && ctx.Redstone.IsPoweringSide(x, y + 1, z, 1)
                ? true
                : facing != 2 && ctx.Redstone.IsPoweringSide(x, y, z - 1, 2)
                    ? true
                    : facing != 3 && ctx.Redstone.IsPoweringSide(x, y, z + 1, 3)
                        ? true
                        : facing != 5 && ctx.Redstone.IsPoweringSide(x + 1, y, z, 5)
                            ? true
                            : facing != 4 && ctx.Redstone.IsPoweringSide(x - 1, y, z, 4)
                                ? true
                                : ctx.Redstone.IsPoweringSide(x, y, z, 0)
                                    ? true
                                    : ctx.Redstone.IsPoweringSide(x, y + 2, z, 1)
                                        ? true
                                        : ctx.Redstone.IsPoweringSide(x, y + 1, z - 1, 2)
                                            ? true
                                            : ctx.Redstone.IsPoweringSide(x, y + 1, z + 1, 3)
                                                ? true
                                                : ctx.Redstone.IsPoweringSide(x - 1, y + 1, z, 4)
                                                    ? true
                                                    : ctx.Redstone.IsPoweringSide(x + 1, y + 1, z, 5);

    public override void onBlockAction(OnBlockActionEvt ctx)
    {
        deaf = true;
        int actionId = ctx.Data1;
        int facing = ctx.Data2;

        if (actionId == 0) // Extending
        {
            if (push(ctx.Level, ctx.X, ctx.Y, ctx.Z, facing))
            {
                ctx.Level.BlockWriter.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, facing | 8);
                ctx.Level.Broadcaster.PlaySoundAtPos(ctx.X + 0.5D, ctx.Y + 0.5D, ctx.Z + 0.5D, "tile.piston.out", 0.5F, Random.Shared.NextSingle() * 0.25F + 0.6F);
            }
        }
        else if (actionId == 1) // Retracting
        {
            int headX = ctx.X + PistonConstants.HEAD_OFFSET_X[facing];
            int headY = ctx.Y + PistonConstants.HEAD_OFFSET_Y[facing];
            int headZ = ctx.Z + PistonConstants.HEAD_OFFSET_Z[facing];

            BlockEntity? entityAtHead = ctx.Level.Entities.GetBlockEntity(headX, headY, headZ);
            if (entityAtHead is BlockEntityPiston extendingPiston)
            {
                extendingPiston.finish();
            }

            ctx.Level.BlockWriter.SetBlockWithoutNotifyingNeighbors(ctx.X, ctx.Y, ctx.Z, MovingPiston.id, facing);
            ctx.Level.Entities.SetBlockEntity(ctx.X, ctx.Y, ctx.Z, BlockPistonMoving.createPistonBlockEntity(id, facing, facing, false, true));
            
            if (sticky)
            {
                int targetX = ctx.X + PistonConstants.HEAD_OFFSET_X[facing] * 2;
                int targetY = ctx.Y + PistonConstants.HEAD_OFFSET_Y[facing] * 2;
                int targetZ = ctx.Z + PistonConstants.HEAD_OFFSET_Z[facing] * 2;
                
                int targetId = ctx.Level.BlocksReader.GetBlockId(targetX, targetY, targetZ);
                int targetMeta = ctx.Level.BlocksReader.GetBlockMeta(targetX, targetY, targetZ);
                bool wasRetractingMovingBlock = false;
                
                if (targetId == MovingPiston.id)
                {
                    BlockEntity? movingTarget = ctx.Level.Entities.GetBlockEntity(targetX, targetY, targetZ);
                    if (movingTarget is BlockEntityPiston movingPistonTarget)
                    {
                        if (movingPistonTarget.getFacing() == facing && movingPistonTarget.isExtending())
                        {
                            movingPistonTarget.finish();
                            targetId = movingPistonTarget.getPushedBlockId();
                            targetMeta = movingPistonTarget.getPushedBlockData();
                            wasRetractingMovingBlock = true;
                        }
                    }
                }

                if (wasRetractingMovingBlock || targetId <= 0 || !canMoveBlock(targetId, ctx.Level, targetX, targetY, targetZ, false) || (Blocks[targetId].getPistonBehavior() != 0 && targetId != Piston.id && targetId != StickyPiston.id))
                {
                    if (!wasRetractingMovingBlock)
                    {
                        deaf = false;
                        ctx.Level.BlockWriter.SetBlock(headX, headY, headZ, 0);
                        deaf = true;
                    }
                }
                else
                {
                    deaf = false;
                    ctx.Level.BlockWriter.SetBlock(targetX, targetY, targetZ, 0);
                    deaf = true;
                    
                    ctx.X += PistonConstants.HEAD_OFFSET_X[facing];
                    ctx.Y += PistonConstants.HEAD_OFFSET_Y[facing];
                    ctx.Z += PistonConstants.HEAD_OFFSET_Z[facing];
                    
                    ctx.Level.BlockWriter.SetBlockWithoutNotifyingNeighbors(ctx.X, ctx.Y, ctx.Z, MovingPiston.id, targetMeta);
                    ctx.Level.Entities.SetBlockEntity(ctx.X, ctx.Y, ctx.Z, BlockPistonMoving.createPistonBlockEntity(targetId, targetMeta, facing, false, false));
                }
            }
            else
            {
                deaf = false;
                ctx.Level.BlockWriter.SetBlock(headX, headY, headZ, 0);
                deaf = true;
            }

            ctx.Level.Broadcaster.PlaySoundAtPos(ctx.X + 0.5D, ctx.Y + 0.5D, ctx.Z + 0.5D, "tile.piston.in", 0.5F, Random.Shared.NextSingle() * 0.15F + 0.6F);
        }

        deaf = false;
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        int meta = iBlockReader.GetBlockMeta(x, y, z);
        if (isExtended(meta))
        {
            switch (getFacing(meta))
            {
                case 0: setBoundingBox(0.0F, 0.25F, 0.0F, 1.0F, 1.0F, 1.0F); break;
                case 1: setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 12.0F / 16.0F, 1.0F); break;
                case 2: setBoundingBox(0.0F, 0.0F, 0.25F, 1.0F, 1.0F, 1.0F); break;
                case 3: setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 12.0F / 16.0F); break;
                case 4: setBoundingBox(0.25F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F); break;
                case 5: setBoundingBox(0.0F, 0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F); break;
            }
        }
        else
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }
    }

    public override void setupRenderBoundingBox() => setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);

    public override void addIntersectingBoundingBox(IBlockReader world, int x, int y, int z, Box box, List<Box> boxes)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
    }

    public override bool isFullCube() => false;

    public static int getFacing(int meta) => meta & 7;

    public static bool isExtended(int meta) => (meta & 8) != 0;

    private static int getFacingForPlacement(IBlockWorldContext world, int x, int y, int z, EntityPlayer player)
    {
        if (MathF.Abs((float)player.x - x) < 2.0F && MathF.Abs((float)player.z - z) < 2.0F)
        {
            double diffY = player.y + 1.82D - player.standingEyeHeight;
            if (diffY - y > 2.0D) return 1;
            if (y - diffY > 0.0D) return 0;
        }

        int playerYaw = MathHelper.Floor(player.yaw * 4.0F / 360.0F + 0.5D) & 3;
        return playerYaw == 0 ? 2 : playerYaw == 1 ? 5 : playerYaw == 2 ? 3 : playerYaw == 3 ? 4 : 0;
    }

    private static bool canMoveBlock(int id, IBlockWorldContext ctx, int x, int y, int z, bool allowBreaking)
    {
        if (id == Obsidian.id) return false;

        if (id != Piston.id && id != StickyPiston.id)
        {
            if (Blocks[id].getHardness() == -1.0F) return false;
            if (Blocks[id].getPistonBehavior() == 2) return false;
            if (!allowBreaking && Blocks[id].getPistonBehavior() == 1) return false;
        }
        else if (isExtended(ctx.BlocksReader.GetBlockMeta(x, y, z)))
        {
            return false;
        }

        BlockEntity? targetEntity = ctx.Entities.GetBlockEntity(x, y, z);
        return targetEntity == null;
    }

   private static bool canExtend(IBlockWorldContext ctx, int x, int y, int z, int dir)
    {
        int checkX = x + PistonConstants.HEAD_OFFSET_X[dir];
        int checkY = y + PistonConstants.HEAD_OFFSET_Y[dir];
        int checkZ = z + PistonConstants.HEAD_OFFSET_Z[dir];
        int pushCount = 0;

        while (true)
        {
            if (pushCount < 13)
            {
                if (checkY <= 0 || checkY >= 127) return false;

                int blockId = ctx.BlocksReader.GetBlockId(checkX, checkY, checkZ);
                if (blockId != 0)
                {
                    if (!canMoveBlock(blockId, ctx, checkX, checkY, checkZ, true)) return false;

                    if (Blocks[blockId].getPistonBehavior() != 1)
                    {
                        if (pushCount == 12) return false;

                        checkX += PistonConstants.HEAD_OFFSET_X[dir];
                        checkY += PistonConstants.HEAD_OFFSET_Y[dir];
                        checkZ += PistonConstants.HEAD_OFFSET_Z[dir];
                        ++pushCount;
                        continue;
                    }
                }
            }
            return true;
        }
    }
    private bool push(IBlockWorldContext ctx, int x, int y, int z, int dir)
    {
        int nextX = x + PistonConstants.HEAD_OFFSET_X[dir];
        int nextY = y + PistonConstants.HEAD_OFFSET_Y[dir];
        int nextZ = z + PistonConstants.HEAD_OFFSET_Z[dir];
        int pushCount = 0;

        while (true)
        {
            int blockId;
            if (pushCount < 13)
            {
                if (nextY <= 0 || nextY >= 127) return false;

                blockId = ctx.BlocksReader.GetBlockId(nextX, nextY, nextZ);
                if (blockId != 0)
                {
                    if (!canMoveBlock(blockId, ctx, nextX, nextY, nextZ, true)) return false;

                    if (Blocks[blockId].getPistonBehavior() != 1)
                    {
                        if (pushCount == 12) return false;

                        nextX += PistonConstants.HEAD_OFFSET_X[dir];
                        nextY += PistonConstants.HEAD_OFFSET_Y[dir];
                        nextZ += PistonConstants.HEAD_OFFSET_Z[dir];
                        ++pushCount;
                        continue;
                    }

                    Blocks[blockId].dropStacks(new OnDropEvt(ctx, nextX, nextY, nextZ, ctx.BlocksReader.GetBlockMeta(nextX, nextY, nextZ)));
                    ctx.BlockWriter.SetBlock(nextX, nextY, nextZ, 0);
                }
            }

            while (nextX != x || nextY != y || nextZ != z)
            {
                int prevX = nextX - PistonConstants.HEAD_OFFSET_X[dir];
                int prevY = nextY - PistonConstants.HEAD_OFFSET_Y[dir];
                int prevZ = nextZ - PistonConstants.HEAD_OFFSET_Z[dir];
                
                int prevBlockId = ctx.BlocksReader.GetBlockId(prevX, prevY, prevZ);
                int prevMeta = ctx.BlocksReader.GetBlockMeta(prevX, prevY, prevZ);
                
                if (prevBlockId == id && prevX == x && prevY == y && prevZ == z)
                {
                    ctx.BlockWriter.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.id, dir | (sticky ? 8 : 0));
                    ctx.Entities.SetBlockEntity(nextX, nextY, nextZ, BlockPistonMoving.createPistonBlockEntity(PistonHead.id, dir | (sticky ? 8 : 0), dir, true, false));
                }
                else
                {
                    ctx.BlockWriter.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.id, prevMeta);
                    ctx.Entities.SetBlockEntity(nextX, nextY, nextZ, BlockPistonMoving.createPistonBlockEntity(prevBlockId, prevMeta, dir, true, false));
                }

                nextX = prevX;
                nextY = prevY;
                nextZ = prevZ;
            }

            return true;
        }
    }
}
