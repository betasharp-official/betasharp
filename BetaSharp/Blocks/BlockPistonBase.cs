using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonBase : Block
{
    private readonly bool sticky;
    private bool deaf;

    public BlockPistonBase(int id, int textureId, bool sticky) : base(id, textureId, Material.Piston)
    {
        this.sticky = sticky;
        setSoundGroup(SoundStoneFootstep);
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

    public override bool onUse(OnUseEvent _) => false;

    public override void onPlaced(OnPlacedEvent @event)
    {
        if (@event.Placer is not EntityPlayer player)
        {
            return;
        }

        int facing = getFacingForPlacement(@event.World, @event.X, @event.Y, @event.Z, player);
        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, facing);

        if (!@event.World.IsRemote)
        {
            checkExtended(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!@event.World.IsRemote && @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z) == null)
        {
            checkExtended(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    private void checkExtended(IWorldContext ctx, int x, int y, int z)
    {
        int meta = ctx.Reader.GetBlockMeta(x, y, z);
        int facing = getFacing(meta);
        bool needsExtension = shouldExtend(ctx, x, y, z, facing);

        if (meta != 7)
        {
            if (needsExtension && !isExtended(meta))
            {
                if (canExtend(ctx, x, y, z, facing))
                {
                    ctx.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing | 8);
                    ctx.Broadcaster.PlayNote(x, y, z, 0, facing); // 0 = Extending
                }
            }
            else if (!needsExtension && isExtended(meta))
            {
                ctx.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing);
                ctx.Broadcaster.PlayNote(x, y, z, 1, facing); // 1 = Retracting
            }
        }
    }

    private bool shouldExtend(IWorldContext ctx, int x, int y, int z, int facing) =>
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

    public override void onBlockAction(OnBlockActionEvent @event)
    {
        deaf = true;
        int actionId = @event.Data1;
        int facing = @event.Data2;

        if (actionId == 0) // Extending
        {
            if (push(@event.World, @event.X, @event.Y, @event.Z, facing))
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, facing | 8);
                @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "tile.piston.out", 0.5F, Random.Shared.NextSingle() * 0.25F + 0.6F);
            }
        }
        else if (actionId == 1) // Retracting
        {
            int headX = @event.X + PistonConstants.HEAD_OFFSET_X[facing];
            int headY = @event.Y + PistonConstants.HEAD_OFFSET_Y[facing];
            int headZ = @event.Z + PistonConstants.HEAD_OFFSET_Z[facing];

            BlockEntity? entityAtHead = @event.World.Entities.GetBlockEntity<BlockEntityPiston>(headX, headY, headZ);
            if (entityAtHead is BlockEntityPiston extendingPiston)
            {
                extendingPiston.finish();
            }

            @event.World.Writer.SetBlockWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, MovingPiston.id, facing);
            @event.World.Entities.SetBlockEntity(@event.X, @event.Y, @event.Z, BlockPistonMoving.createPistonBlockEntity(id, facing, facing, false, true));

            if (sticky)
            {
                int targetX = @event.X + PistonConstants.HEAD_OFFSET_X[facing] * 2;
                int targetY = @event.Y + PistonConstants.HEAD_OFFSET_Y[facing] * 2;
                int targetZ = @event.Z + PistonConstants.HEAD_OFFSET_Z[facing] * 2;

                int targetId = @event.World.Reader.GetBlockId(targetX, targetY, targetZ);
                int targetMeta = @event.World.Reader.GetBlockMeta(targetX, targetY, targetZ);
                bool wasRetractingMovingBlock = false;

                if (targetId == MovingPiston.id)
                {
                    BlockEntity? movingTarget = @event.World.Entities.GetBlockEntity<BlockEntityPiston>(targetX, targetY, targetZ);
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

                if (wasRetractingMovingBlock || targetId <= 0 || !canMoveBlock(targetId, @event.World, targetX, targetY, targetZ, false) || (Blocks[targetId].getPistonBehavior() != 0 && targetId != Piston.id && targetId != StickyPiston.id))
                {
                    if (!wasRetractingMovingBlock)
                    {
                        deaf = false;
                        @event.World.Writer.SetBlock(headX, headY, headZ, 0);
                        deaf = true;
                    }
                }
                else
                {
                    deaf = false;
                    @event.World.Writer.SetBlock(targetX, targetY, targetZ, 0);
                    deaf = true;

                    int x = @event.X;
                    int y = @event.Y;
                    int z = @event.Z;

                    x += PistonConstants.HEAD_OFFSET_X[facing];
                    y += PistonConstants.HEAD_OFFSET_Y[facing];
                    z += PistonConstants.HEAD_OFFSET_Z[facing];

                    @event.World.Writer.SetBlockWithoutNotifyingNeighbors(x, y, z, MovingPiston.id, targetMeta);
                    @event.World.Entities.SetBlockEntity(x, y, z, BlockPistonMoving.createPistonBlockEntity(targetId, targetMeta, facing, false, false));
                }
            }
            else
            {
                deaf = false;
                @event.World.Writer.SetBlock(headX, headY, headZ, 0);
                deaf = true;
            }

            @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "tile.piston.in", 0.5F, Random.Shared.NextSingle() * 0.15F + 0.6F);
        }

        deaf = false;
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
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

    public override void addIntersectingBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z, Box box, List<Box> boxes)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        base.addIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
    }

    public override bool isFullCube() => false;

    public static int getFacing(int meta) => meta & 7;

    public static bool isExtended(int meta) => (meta & 8) != 0;

    private static int getFacingForPlacement(IWorldContext world, int x, int y, int z, EntityPlayer player)
    {
        if (MathF.Abs((float)player.x - x) < 2.0F && MathF.Abs((float)player.z - z) < 2.0F)
        {
            double diffY = player.y + 1.82D - player.standingEyeHeight;
            if (diffY - y > 2.0D)
            {
                return 1;
            }

            if (y - diffY > 0.0D)
            {
                return 0;
            }
        }

        int playerYaw = MathHelper.Floor(player.yaw * 4.0F / 360.0F + 0.5D) & 3;
        return playerYaw == 0 ? 2 : playerYaw == 1 ? 5 : playerYaw == 2 ? 3 : playerYaw == 3 ? 4 : 0;
    }

    private static bool canMoveBlock(int id, IWorldContext ctx, int x, int y, int z, bool allowBreaking)
    {
        if (id == Obsidian.id)
        {
            return false;
        }

        if (id != Piston.id && id != StickyPiston.id)
        {
            if (Blocks[id].getHardness() == -1.0F)
            {
                return false;
            }

            if (Blocks[id].getPistonBehavior() == 2)
            {
                return false;
            }

            if (!allowBreaking && Blocks[id].getPistonBehavior() == 1)
            {
                return false;
            }
        }
        else if (isExtended(ctx.Reader.GetBlockMeta(x, y, z)))
        {
            return false;
        }

        BlockEntity? targetEntity = ctx.Entities.GetBlockEntity<BlockEntity>(x, y, z);
        return targetEntity == null;
    }

    private static bool canExtend(IWorldContext ctx, int x, int y, int z, int dir)
    {
        int checkX = x + PistonConstants.HEAD_OFFSET_X[dir];
        int checkY = y + PistonConstants.HEAD_OFFSET_Y[dir];
        int checkZ = z + PistonConstants.HEAD_OFFSET_Z[dir];
        int pushCount = 0;

        while (true)
        {
            if (pushCount < 13)
            {
                if (checkY <= 0 || checkY >= 127)
                {
                    return false;
                }

                int blockId = ctx.Reader.GetBlockId(checkX, checkY, checkZ);
                if (blockId != 0)
                {
                    if (!canMoveBlock(blockId, ctx, checkX, checkY, checkZ, true))
                    {
                        return false;
                    }

                    if (Blocks[blockId].getPistonBehavior() != 1)
                    {
                        if (pushCount == 12)
                        {
                            return false;
                        }

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

    private bool push(IWorldContext ctx, int x, int y, int z, int dir)
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
                if (nextY <= 0 || nextY >= 127)
                {
                    return false;
                }

                blockId = ctx.Reader.GetBlockId(nextX, nextY, nextZ);
                if (blockId != 0)
                {
                    if (!canMoveBlock(blockId, ctx, nextX, nextY, nextZ, true))
                    {
                        return false;
                    }

                    if (Blocks[blockId].getPistonBehavior() != 1)
                    {
                        if (pushCount == 12)
                        {
                            return false;
                        }

                        nextX += PistonConstants.HEAD_OFFSET_X[dir];
                        nextY += PistonConstants.HEAD_OFFSET_Y[dir];
                        nextZ += PistonConstants.HEAD_OFFSET_Z[dir];
                        ++pushCount;
                        continue;
                    }

                    Blocks[blockId].dropStacks(new OnDropEvent(ctx, nextX, nextY, nextZ, ctx.Reader.GetBlockMeta(nextX, nextY, nextZ)));
                    ctx.Writer.SetBlock(nextX, nextY, nextZ, 0);
                }
            }

            while (nextX != x || nextY != y || nextZ != z)
            {
                int prevX = nextX - PistonConstants.HEAD_OFFSET_X[dir];
                int prevY = nextY - PistonConstants.HEAD_OFFSET_Y[dir];
                int prevZ = nextZ - PistonConstants.HEAD_OFFSET_Z[dir];

                int prevBlockId = ctx.Reader.GetBlockId(prevX, prevY, prevZ);
                int prevMeta = ctx.Reader.GetBlockMeta(prevX, prevY, prevZ);

                if (prevBlockId == id && prevX == x && prevY == y && prevZ == z)
                {
                    ctx.Writer.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.id, dir | (sticky ? 8 : 0));
                    ctx.Entities.SetBlockEntity(nextX, nextY, nextZ, BlockPistonMoving.createPistonBlockEntity(PistonHead.id, dir | (sticky ? 8 : 0), dir, true, false));
                }
                else
                {
                    ctx.Writer.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.id, prevMeta);
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
