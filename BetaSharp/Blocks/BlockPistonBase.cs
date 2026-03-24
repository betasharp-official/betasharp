using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonBase : Block
{
    private readonly bool _sticky;
    private bool _deaf;

    public BlockPistonBase(int id, int textureId, bool sticky) : base(id, textureId, Material.Piston)
    {
        _sticky = sticky;
        setSoundGroup(SoundStoneFootstep);
        SetHardness(0.5F);
    }

    public int getTopTexture() => _sticky ? 106 : 107;

    public override int GetTexture(Side side) =>
        side switch
        {
            Side.Up => getTopTexture(),
            Side.Down => 109,
            _ => 108
        };

    public override int GetTexture(Side side, int meta)
    {
        int facing = getFacing(meta);
        if (facing > 5)
        {
            return TextureId;
        }

        if (side != facing.ToSide())
        {
            return side == SideExtensions.OppositeFace(facing.ToSide()) ? 109 : 108;
        }

        if (isExtended(meta) ||
            BoundingBox is
                not
                {
                    MinX: <= 0.0D,
                    MinY: <= 0.0D,
                    MinZ: <= 0.0D,
                    MaxX: >= 1.0D,
                    MaxY: >= 1.0D,
                    MaxZ: >= 1.0D
                })
        {
            return 110;
        }

        return TextureId;
    }

    public override BlockRendererType GetRenderType() => BlockRendererType.PistonBase;

    public override bool IsOpaque() => false;

    public override bool OnUse(OnUseEvent _) => false;

    public override void OnPlaced(OnPlacedEvent @event)
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

    public override void NeighborUpdate(OnTickEvent @event)
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
        if (meta == 7)
        {
            return;
        }

        switch (shouldExtend(ctx, x, y, z, facing))
        {
            case true when !isExtended(meta):
                {
                    if (canExtend(ctx, x, y, z, facing))
                    {
                        ctx.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing | 8);
                        ctx.Broadcaster.PlayNote(x, y, z, 0, facing); // 0 = Extending
                    }

                    break;
                }
            case false when isExtended(meta):
                ctx.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing);
                ctx.Broadcaster.PlayNote(x, y, z, 1, facing); // 1 = Retracting
                break;
        }
    }

    private bool shouldExtend(IWorldContext ctx, int x, int y, int z, int facing) =>
        (facing != 0 && ctx.Redstone.IsPoweringSide(x, y - 1, z, 0)) || (facing != 1 && ctx.Redstone.IsPoweringSide(x, y + 1, z, 1)) || (facing != 2 && ctx.Redstone.IsPoweringSide(x, y, z - 1, 2)) ||
        (facing != 3 && ctx.Redstone.IsPoweringSide(x, y, z + 1, 3)) ||
        (facing != 5 && ctx.Redstone.IsPoweringSide(x + 1, y, z, 5)) ||
        (facing != 4 && ctx.Redstone.IsPoweringSide(x - 1, y, z, 4)) ||
        ctx.Redstone.IsPoweringSide(x, y, z, 0) || ctx.Redstone.IsPoweringSide(x, y + 2, z, 1) ||
        ctx.Redstone.IsPoweringSide(x, y + 1, z - 1, 2) ||
        ctx.Redstone.IsPoweringSide(x, y + 1, z + 1, 3) ||
        ctx.Redstone.IsPoweringSide(x - 1, y + 1, z, 4) ||
        ctx.Redstone.IsPoweringSide(x + 1, y + 1, z, 5);

    public override void OnBlockAction(OnBlockActionEvent @event)
    {
        _deaf = true;
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
            int headX = @event.X + PistonConstants.HeadOffsetX(facing);
            int headY = @event.Y + PistonConstants.HeadOffsetY(facing);
            int headZ = @event.Z + PistonConstants.HeadOffsetZ(facing);

            BlockEntity? entityAtHead = @event.World.Entities.GetBlockEntity<BlockEntityPiston>(headX, headY, headZ);
            if (entityAtHead is BlockEntityPiston extendingPiston)
            {
                extendingPiston.finish();
            }

            @event.World.Writer.SetBlockWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, MovingPiston.Id, facing);
            @event.World.Entities.SetBlockEntity(@event.X, @event.Y, @event.Z, BlockPistonMoving.createPistonBlockEntity(Id, facing, facing, false, true));

            if (_sticky)
            {
                int targetX = @event.X + PistonConstants.HeadOffsetX(facing) * 2;
                int targetY = @event.Y + PistonConstants.HeadOffsetY(facing) * 2;
                int targetZ = @event.Z + PistonConstants.HeadOffsetZ(facing) * 2;

                int targetId = @event.World.Reader.GetBlockId(targetX, targetY, targetZ);
                int targetMeta = @event.World.Reader.GetBlockMeta(targetX, targetY, targetZ);
                bool wasRetractingMovingBlock = false;

                if (targetId == MovingPiston.Id)
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

                if (wasRetractingMovingBlock || targetId <= 0 || !canMoveBlock(targetId, @event.World, targetX, targetY, targetZ, false) || (Blocks[targetId].GetPistonBehavior() != 0 && targetId != Piston.Id && targetId != StickyPiston.Id))
                {
                    if (!wasRetractingMovingBlock)
                    {
                        _deaf = false;
                        @event.World.Writer.SetBlock(headX, headY, headZ, 0);
                        _deaf = true;
                    }
                }
                else
                {
                    _deaf = false;
                    @event.World.Writer.SetBlock(targetX, targetY, targetZ, 0);
                    _deaf = true;

                    int x = @event.X;
                    int y = @event.Y;
                    int z = @event.Z;

                    x += PistonConstants.HeadOffsetX(facing);
                    y += PistonConstants.HeadOffsetY(facing);
                    z += PistonConstants.HeadOffsetZ(facing);

                    @event.World.Writer.SetBlockWithoutNotifyingNeighbors(x, y, z, MovingPiston.Id, targetMeta);
                    @event.World.Entities.SetBlockEntity(x, y, z, BlockPistonMoving.createPistonBlockEntity(targetId, targetMeta, facing, false, false));
                }
            }
            else
            {
                _deaf = false;
                @event.World.Writer.SetBlock(headX, headY, headZ, 0);
                _deaf = true;
            }

            @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "tile.piston.in", 0.5F, Random.Shared.NextSingle() * 0.15F + 0.6F);
        }

        _deaf = false;
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
        if (isExtended(meta))
        {
            switch (getFacing(meta))
            {
                case (int)Side.Down: SetBoundingBox(0.0F, 0.25F, 0.0F, 1.0F, 1.0F, 1.0F); break;
                case (int)Side.Up: SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 12.0F / 16.0F, 1.0F); break;
                case (int)Side.North: SetBoundingBox(0.0F, 0.0F, 0.25F, 1.0F, 1.0F, 1.0F); break;
                case (int)Side.South: SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 12.0F / 16.0F); break;
                case (int)Side.West: SetBoundingBox(0.25F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F); break;
                case (int)Side.East: SetBoundingBox(0.0F, 0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F); break;
            }
        }
        else
        {
            SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }
    }

    public override void SetupRenderBoundingBox() => SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);

    public override void AddIntersectingBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z, Box box, List<Box> boxes)
    {
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
    }

    public override bool IsFullCube() => false;

    public static int getFacing(int meta) => meta & 7;

    public static bool isExtended(int meta) => (meta & 8) != 0;

    private static int getFacingForPlacement(IWorldContext world, int x, int y, int z, EntityPlayer player)
    {
        if (MathF.Abs((float)player.x - x) < 2.0F && MathF.Abs((float)player.z - z) < 2.0F)
        {
            double diffY = player.y + 1.82D - player.standingEyeHeight;
            if (diffY - y > 2.0D)
            {
                return (int)Side.Up;
            }

            if (y - diffY > 0.0D)
            {
                return (int)Side.Down;
            }
        }

        int playerYaw = MathHelper.Floor(player.yaw * 4.0F / 360.0F + 0.5D) & 3;
        return playerYaw switch
        {
            0 => (int)Side.North,
            1 => (int)Side.East,
            2 => (int)Side.South,
            3 => (int)Side.West,
            _ => (int)Side.Down
        };
    }

    private static bool canMoveBlock(int id, IWorldContext ctx, int x, int y, int z, bool allowBreaking)
    {
        if (id == Obsidian.Id)
        {
            return false;
        }

        if (id != Piston.Id && id != StickyPiston.Id)
        {
            if (Blocks[id]!.GetHardness() == -1.0F)
            {
                return false;
            }

            if (Blocks[id]!.GetPistonBehavior() == 2)
            {
                return false;
            }

            if (!allowBreaking && Blocks[id]!.GetPistonBehavior() == 1)
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
        int checkX = x + PistonConstants.HeadOffsetX(dir);
        int checkY = y + PistonConstants.HeadOffsetY(dir);
        int checkZ = z + PistonConstants.HeadOffsetZ(dir);
        int pushCount = 0;

        while (true)
        {
            if (pushCount >= 13)
            {
                return true;
            }

            if (checkY is <= 0 or >= 127)
            {
                return false;
            }

            int blockId = ctx.Reader.GetBlockId(checkX, checkY, checkZ);
            if (blockId == 0)
            {
                return true;
            }

            if (!canMoveBlock(blockId, ctx, checkX, checkY, checkZ, true))
            {
                return false;
            }

            if (Blocks[blockId]!.GetPistonBehavior() == 1)
            {
                return true;
            }

            if (pushCount == 12)
            {
                return false;
            }

            checkX += PistonConstants.HeadOffsetX(dir);
            checkY += PistonConstants.HeadOffsetY(dir);
            checkZ += PistonConstants.HeadOffsetZ(dir);
            ++pushCount;
        }
    }

    private bool push(IWorldContext ctx, int x, int y, int z, int dir)
    {
        int nextX = x + PistonConstants.HeadOffsetX(dir);
        int nextY = y + PistonConstants.HeadOffsetY(dir);
        int nextZ = z + PistonConstants.HeadOffsetZ(dir);
        int pushCount = 0;

        while (true)
        {
            if (pushCount < 13)
            {
                if (nextY is <= 0 or >= 127)
                {
                    return false;
                }

                int blockId = ctx.Reader.GetBlockId(nextX, nextY, nextZ);
                if (blockId != 0)
                {
                    if (!canMoveBlock(blockId, ctx, nextX, nextY, nextZ, true))
                    {
                        return false;
                    }

                    if (Blocks[blockId]!.GetPistonBehavior() != 1)
                    {
                        if (pushCount == 12)
                        {
                            return false;
                        }

                        nextX += PistonConstants.HeadOffsetX(dir);
                        nextY += PistonConstants.HeadOffsetY(dir);
                        nextZ += PistonConstants.HeadOffsetZ(dir);
                        ++pushCount;
                        continue;
                    }

                    Blocks[blockId]?.DropStacks(new OnDropEvent(ctx, nextX, nextY, nextZ, ctx.Reader.GetBlockMeta(nextX, nextY, nextZ)));
                    ctx.Writer.SetBlock(nextX, nextY, nextZ, 0);
                }
            }

            while (nextX != x || nextY != y || nextZ != z)
            {
                int prevX = nextX - PistonConstants.HeadOffsetX(dir);
                int prevY = nextY - PistonConstants.HeadOffsetY(dir);
                int prevZ = nextZ - PistonConstants.HeadOffsetZ(dir);

                int prevBlockId = ctx.Reader.GetBlockId(prevX, prevY, prevZ);
                int prevMeta = ctx.Reader.GetBlockMeta(prevX, prevY, prevZ);

                if (prevBlockId == Id && prevX == x && prevY == y && prevZ == z)
                {
                    ctx.Writer.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.Id, dir | (_sticky ? 8 : 0));
                    ctx.Entities.SetBlockEntity(nextX, nextY, nextZ, BlockPistonMoving.createPistonBlockEntity(PistonHead.Id, dir | (_sticky ? 8 : 0), dir, true, false));
                }
                else
                {
                    ctx.Writer.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.Id, prevMeta);
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
