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
        SetSoundGroup(SoundStoneFootstep);
        SetHardness(0.5F);
    }

    public int GetTopTexture() => _sticky ? 106 : 107;

    public override int GetTexture(Side side) =>
        side switch
        {
            Side.Up => GetTopTexture(),
            Side.Down => 109,
            _ => 108
        };

    public override int GetTexture(Side side, int meta)
    {
        Side facing = GetFacing(meta);
        if (facing.ToInt() > 5)
        {
            return TextureId;
        }

        if (side != facing)
        {
            return side == SideExtensions.OppositeFace(facing) ? 109 : 108;
        }

        if (IsExtended(meta) ||
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

    public override bool IsOpaque => false;

    public override bool OnUse(OnUseEvent _) => false;

    public override void OnPlaced(OnPlacedEvent @event)
    {
        if (@event.Placer is not EntityPlayer player)
        {
            return;
        }

        Side facing = GetFacingForPlacement(@event.X, @event.Y, @event.Z, player);
        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, facing.ToInt());

        if (!@event.World.IsRemote)
        {
            CheckExtended(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!@event.World.IsRemote && @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z) == null)
        {
            CheckExtended(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    private void CheckExtended(IWorldContext ctx, int x, int y, int z)
    {
        int meta = ctx.Reader.GetBlockMeta(x, y, z);
        Side facing = GetFacing(meta);
        if (meta == 7)
        {
            return;
        }

        switch (ShouldExtend(ctx, x, y, z, facing))
        {
            case true when !IsExtended(meta):
                {
                    if (CanExtend(ctx, x, y, z, facing))
                    {
                        ctx.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing.ToInt() | 8);
                        ctx.Broadcaster.PlayNote(x, y, z, 0, facing.ToInt()); // 0 = Extending
                    }

                    break;
                }
            case false when IsExtended(meta):
                ctx.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing.ToInt());
                ctx.Broadcaster.PlayNote(x, y, z, 1, facing.ToInt()); // 1 = Retracting
                break;
        }
    }

    private static bool ShouldExtend(IWorldContext ctx, int x, int y, int z, Side facing) =>
        (facing != Side.Down && ctx.Redstone.IsPoweringSide(x, y - 1, z, 0)) ||
        (facing != Side.Up && ctx.Redstone.IsPoweringSide(x, y + 1, z, 1)) ||
        (facing != Side.North && ctx.Redstone.IsPoweringSide(x, y, z - 1, 2)) ||
        (facing != Side.South && ctx.Redstone.IsPoweringSide(x, y, z + 1, 3)) ||
        (facing != Side.East && ctx.Redstone.IsPoweringSide(x + 1, y, z, 5)) ||
        (facing != Side.West && ctx.Redstone.IsPoweringSide(x - 1, y, z, 4)) ||
        ctx.Redstone.IsPoweringSide(x, y, z, 0) || ctx.Redstone.IsPoweringSide(x, y + 2, z, 1) ||
        ctx.Redstone.IsPoweringSide(x, y + 1, z - 1, 2) ||
        ctx.Redstone.IsPoweringSide(x, y + 1, z + 1, 3) ||
        ctx.Redstone.IsPoweringSide(x - 1, y + 1, z, 4) ||
        ctx.Redstone.IsPoweringSide(x + 1, y + 1, z, 5);

    public override void OnBlockAction(OnBlockActionEvent @event)
    {
        _deaf = true;
        int actionId = @event.Data1;
        Side facing = @event.Data2.ToSide();

        switch (actionId)
        {
            // Extending
            case 0:
                {
                    if (Push(@event.World, @event.X, @event.Y, @event.Z, facing))
                    {
                        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, facing.ToInt() | 8);
                        @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "tile.piston.out", 0.5F, Random.Shared.NextSingle() * 0.25F + 0.6F);
                    }

                    break;
                }
            // Retracting
            case 1:
                {
                    int headX = @event.X + PistonConstants.HeadOffsetX(facing);
                    int headY = @event.Y + PistonConstants.HeadOffsetY(facing);
                    int headZ = @event.Z + PistonConstants.HeadOffsetZ(facing);

                    BlockEntity? entityAtHead = @event.World.Entities.GetBlockEntity<BlockEntityPiston>(headX, headY, headZ);
                    if (entityAtHead is BlockEntityPiston extendingPiston)
                    {
                        extendingPiston.Finish();
                    }

                    @event.World.Writer.SetBlockWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, MovingPiston.Id, facing.ToInt());
                    @event.World.Entities.SetBlockEntity(@event.X, @event.Y, @event.Z, BlockPistonMoving.CreatePistonBlockEntity(Id, facing.ToInt(), facing, false, true));

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
                                if (movingPistonTarget.GetFacing() == facing && movingPistonTarget.IsExtending())
                                {
                                    movingPistonTarget.Finish();
                                    targetId = movingPistonTarget.GetPushedBlockId();
                                    targetMeta = movingPistonTarget.GetPushedBlockData();
                                    wasRetractingMovingBlock = true;
                                }
                            }
                        }

                        if (wasRetractingMovingBlock || targetId <= 0 || !CanMoveBlock(targetId, @event.World, targetX, targetY, targetZ, false) ||
                            (Blocks[targetId]!.PistonBehavior != 0 && targetId != Piston.Id && targetId != StickyPiston.Id))
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
                            @event.World.Entities.SetBlockEntity(x, y, z, BlockPistonMoving.CreatePistonBlockEntity(targetId, targetMeta, facing, false, false));
                        }
                    }
                    else
                    {
                        _deaf = false;
                        @event.World.Writer.SetBlock(headX, headY, headZ, 0);
                        _deaf = true;
                    }

                    @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "tile.piston.in", 0.5F, Random.Shared.NextSingle() * 0.15F + 0.6F);
                    break;
                }
        }

        _deaf = false;
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
        if (IsExtended(meta))
        {
            switch (GetFacing(meta))
            {
                case Side.Down: SetBoundingBox(0.0F, 0.25F, 0.0F, 1.0F, 1.0F, 1.0F); break;
                case Side.Up: SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 12.0F / 16.0F, 1.0F); break;
                case Side.North: SetBoundingBox(0.0F, 0.0F, 0.25F, 1.0F, 1.0F, 1.0F); break;
                case Side.South: SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 12.0F / 16.0F); break;
                case Side.West: SetBoundingBox(0.25F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F); break;
                case Side.East: SetBoundingBox(0.0F, 0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F); break;
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

    public override bool IsFullCube => false;

    public static Side GetFacing(int meta) => (meta & 7).ToSide();

    public static bool IsExtended(int meta) => (meta & 8) != 0;

    private static Side GetFacingForPlacement(int x, int y, int z, EntityPlayer player)
    {
        if (MathF.Abs((float)player.x - x) < 2.0F && MathF.Abs((float)player.z - z) < 2.0F)
        {
            double diffY = player.y + 1.82D - player.standingEyeHeight;
            if (diffY - y > 2.0D)
            {
                return Side.Up;
            }

            if (y - diffY > 0.0D)
            {
                return (int)Side.Down;
            }
        }

        int playerYaw = MathHelper.Floor(player.yaw * 4.0F / 360.0F + 0.5D) & 3;
        return playerYaw switch
        {
            0 => Side.North,
            1 => Side.East,
            2 => Side.South,
            3 => Side.West,
            _ => Side.Down
        };
    }

    private static bool CanMoveBlock(int id, IWorldContext ctx, int x, int y, int z, bool allowBreaking)
    {
        if (id == Obsidian.Id)
        {
            return false;
        }

        if (id != Piston.Id && id != StickyPiston.Id)
        {
            if (Math.Abs(Blocks[id]!.GetHardness() - -1.0F) < 0.001F)
            {
                return false;
            }

            if (Blocks[id]!.PistonBehavior == PistonBehavior.UnPushable)
            {
                return false;
            }

            if (!allowBreaking && Blocks[id]!.PistonBehavior == PistonBehavior.Destroy)
            {
                return false;
            }
        }
        else if (IsExtended(ctx.Reader.GetBlockMeta(x, y, z)))
        {
            return false;
        }

        BlockEntity? targetEntity = ctx.Entities.GetBlockEntity<BlockEntity>(x, y, z);
        return targetEntity == null;
    }

    private static bool CanExtend(IWorldContext ctx, int x, int y, int z, Side dir)
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

            if (!CanMoveBlock(blockId, ctx, checkX, checkY, checkZ, true))
            {
                return false;
            }

            if (Blocks[blockId]!.PistonBehavior == PistonBehavior.Destroy)
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

    private bool Push(IWorldContext ctx, int x, int y, int z, Side dir)
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
                    if (!CanMoveBlock(blockId, ctx, nextX, nextY, nextZ, true))
                    {
                        return false;
                    }

                    if (Blocks[blockId]!.PistonBehavior != PistonBehavior.Destroy)
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
                    ctx.Writer.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.Id, dir.ToInt() | (_sticky ? 8 : 0));
                    ctx.Entities.SetBlockEntity(nextX, nextY, nextZ, BlockPistonMoving.CreatePistonBlockEntity(PistonHead.Id, dir.ToInt() | (_sticky ? 8 : 0), dir, true, false));
                }
                else
                {
                    ctx.Writer.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.Id, prevMeta);
                    ctx.Entities.SetBlockEntity(nextX, nextY, nextZ, BlockPistonMoving.CreatePistonBlockEntity(prevBlockId, prevMeta, dir, true, false));
                }

                nextX = prevX;
                nextY = prevY;
                nextZ = prevZ;
            }

            return true;
        }
    }
}
