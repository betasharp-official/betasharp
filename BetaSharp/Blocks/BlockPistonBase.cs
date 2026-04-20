using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonBase : Block
{
    private const int MaxPushCount = 12;
    private readonly bool _sticky;
    private bool _deaf;

    public BlockPistonBase(int id, int textureId, bool sticky) : base(id, textureId, Material.Piston)
    {
        _sticky = sticky;
        SetSoundGroup(SoundStoneFootstep);
        SetHardness(0.5F);
    }

    public int GetTopTexture() => _sticky ? BlockTextures.PistonTopSticky : BlockTextures.PistonTopNormal;

    public override int GetTexture(Side side) =>
        side switch
        {
            Side.Up => GetTopTexture(),
            Side.Down => BlockTextures.PistonBottom,
            _ => BlockTextures.PistonSide
        };

    public override int GetTexture(Side side, int meta)
    {
        Side facing = GetFacing(meta).ToSide();
        if (facing > Side.East) return TextureId;
        if (side == facing)
            return !IsExtended(meta) &&
             BoundingBox is { MinX: <= 0.0D, MinY: <= 0.0D, MinZ: <= 0.0D, MaxX: >= 1.0D, MaxY: >= 1.0D, MaxZ: >= 1.0D } ?
             TextureId :
             BlockTextures.PistonExtensionSide;
        return side == facing.OppositeFace() ? BlockTextures.PistonBottom : BlockTextures.PistonSide;
    }

    public override BlockRendererType GetRenderType() => BlockRendererType.PistonBase;

    public override bool IsOpaque() => false;

    public override bool OnUse(OnUseEvent _) => false;

    public override void OnPlaced(OnPlacedEvent @event)
    {
        if (@event.Placer is EntityPlayer player)
        {
            int facing = GetFacingForPlacement(@event.X, @event.Y, @event.Z, player);
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, facing);
        }

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

    public override void OnTick(OnTickEvent @event)
    {
        if (!@event.World.IsRemote && @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z) == null)
        {
            CheckExtended(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    private static void CheckExtended(IWorldContext ctx, int x, int y, int z)
    {
        int meta = ctx.Reader.GetBlockMeta(x, y, z);
        int facing = GetFacing(meta);
        bool needsExtension = ShouldExtend(ctx, x, y, z, facing);

        if (meta == 7)
        {
            return;
        }

        switch (needsExtension)
        {
            case true when !IsExtended(meta):
                if (!CanExtend(ctx, x, y, z, facing))
                {
                    return;
                }

                ctx.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing | 8);
                ctx.Broadcaster.PlayNote(x, y, z, 0, facing); // 0 = Extending
                break;
            case false when IsExtended(meta):
                ctx.Writer.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, facing);
                ctx.Broadcaster.PlayNote(x, y, z, 1, facing); // 1 = Retracting
                break;
        }
    }

    private static bool ShouldExtend(IWorldContext ctx, int x, int y, int z, int facing) =>
        (facing != 0 && ctx.Redstone.IsPoweringSide(x, y - 1, z, 0)) ||
        (facing != 1 && ctx.Redstone.IsPoweringSide(x, y + 1, z, 1)) ||
        (facing != 2 && ctx.Redstone.IsPoweringSide(x, y, z - 1, 2)) ||
        (facing != 3 && ctx.Redstone.IsPoweringSide(x, y, z + 1, 3)) ||
        (facing != 4 && ctx.Redstone.IsPoweringSide(x - 1, y, z, 4)) ||
        (facing != 5 && ctx.Redstone.IsPoweringSide(x + 1, y, z, 5)) ||
        ctx.Redstone.IsPoweringSide(x, y, z, 0) ||
        ctx.Redstone.IsPoweringSide(x, y + 2, z, 1) ||
        ctx.Redstone.IsPoweringSide(x, y + 1, z - 1, 2) ||
        ctx.Redstone.IsPoweringSide(x, y + 1, z + 1, 3) ||
        ctx.Redstone.IsPoweringSide(x - 1, y + 1, z, 4) ||
        ctx.Redstone.IsPoweringSide(x + 1, y + 1, z, 5);

    public override void OnBlockAction(OnBlockActionEvent @event)
    {
        _deaf = true;
        int actionId = @event.Data1;
        int facing = @event.Data2;

        switch (actionId)
        {
            case 0: // Extending
                if (Push(@event.World, @event.X, @event.Y, @event.Z, facing))
                {
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, facing | 8);
                    @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "tile.piston.out", 0.5F, Random.Shared.NextSingle() * 0.25F + 0.6F);
                }

                break;
            case 1: // Retracting

                int headX = @event.X + PistonConstants.HeadOffsetX[facing];
                int headY = @event.Y + PistonConstants.HeadOffsetY[facing];
                int headZ = @event.Z + PistonConstants.HeadOffsetZ[facing];

                BlockEntity? entityAtHead = @event.World.Entities.GetBlockEntity<BlockEntityPiston>(headX, headY, headZ);
                if (entityAtHead is BlockEntityPiston extendingPiston)
                {
                    extendingPiston.Finish();
                }

                @event.World.Writer.SetBlockWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, MovingPiston.ID, facing);
                @event.World.Entities.SetBlockEntity(@event.X, @event.Y, @event.Z, BlockPistonMoving.CreatePistonBlockEntity(ID, facing, facing, false, true));

                if (_sticky)
                {
                    int targetX = headX + PistonConstants.HeadOffsetX[facing];
                    int targetY = headY + PistonConstants.HeadOffsetY[facing];
                    int targetZ = headZ + PistonConstants.HeadOffsetZ[facing];

                    int targetId = @event.World.Reader.GetBlockId(targetX, targetY, targetZ);
                    int targetMeta = @event.World.Reader.GetBlockMeta(targetX, targetY, targetZ);
                    bool stickySpit = false;

                    if (targetId == MovingPiston.ID)
                    {
                        BlockEntity? movingTarget = @event.World.Entities.GetBlockEntity<BlockEntityPiston>(targetX, targetY, targetZ);
                        if (movingTarget is BlockEntityPiston movingPistonTarget && movingPistonTarget.Facing == facing && movingPistonTarget.IsExtending)
                        {
                            if (movingPistonTarget.IsExtensionIncomplete)
                            {
                                movingPistonTarget.AbandonExtensionToStaticBlock();
                                stickySpit = true;
                            }
                            else
                            {
                                movingPistonTarget.Finish();
                                targetId = movingPistonTarget.PushedBlockId;
                                targetMeta = movingPistonTarget.PushedBlockData;
                            }
                        }
                    }

                    if (stickySpit)
                    {
                        _deaf = false;
                        @event.World.Writer.SetBlock(headX, headY, headZ, 0);
                        _deaf = true;
                    }
                    else if (targetId > 0 && CanMoveBlock(targetId, @event.World, targetX, targetY, targetZ, false) &&
                             (Blocks[targetId].GetPistonBehavior() == 0 || targetId == Piston.ID || targetId == StickyPiston.ID))
                    {
                        _deaf = false;
                        @event.World.Writer.SetBlock(headX, headY, headZ, 0);
                        @event.World.Writer.SetBlock(targetX, targetY, targetZ, 0);
                        _deaf = true;

                        @event.World.Writer.SetBlockWithoutNotifyingNeighbors(headX, headY, headZ, MovingPiston.ID, targetMeta);
                        @event.World.Entities.SetBlockEntity(headX, headY, headZ, BlockPistonMoving.CreatePistonBlockEntity(targetId, targetMeta, facing, false, false));
                    }
                    else
                    {
                        _deaf = false;
                        @event.World.Writer.SetBlock(headX, headY, headZ, 0);
                        _deaf = true;
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

        _deaf = false;
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
        if (IsExtended(meta))
        {
            switch (GetFacing(meta))
            {
                case 0: SetBoundingBox(0.0F, 0.25F, 0.0F, 1.0F, 1.0F, 1.0F); break;
                case 1: SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 12.0F / 16.0F, 1.0F); break;
                case 2: SetBoundingBox(0.0F, 0.0F, 0.25F, 1.0F, 1.0F, 1.0F); break;
                case 3: SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 12.0F / 16.0F); break;
                case 4: SetBoundingBox(0.25F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F); break;
                case 5: SetBoundingBox(0.0F, 0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F); break;
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

    public static int GetFacing(int meta) => meta & 7;

    public static bool IsExtended(int meta) => (meta & 8) != 0;

    private static int GetFacingForPlacement(int x, int y, int z, EntityPlayer player)
    {
        if (MathF.Abs((float)player.X - x) < 2.0F && MathF.Abs((float)player.Z - z) < 2.0F)
        {
            double diffY = player.Y + 1.82D - player.StandingEyeHeight;
            if (diffY - y > 2.0D)
            {
                return 1;
            }

            if (y - diffY > 0.0D)
            {
                return 0;
            }
        }

        int playerYaw = MathHelper.Floor(player.Yaw * 4.0F / 360.0F + 0.5D) & 3;
        return playerYaw switch
        {
            0 => 2,
            1 => 5,
            2 => 3,
            3 => 4,
            _ => 0
        };
    }

    private static bool CanMoveBlock(int id, IWorldContext ctx, int x, int y, int z, bool allowBreaking)
    {
        if (id == Obsidian.ID)
        {
            return false;
        }

        if (id != Piston.ID && id != StickyPiston.ID)
        {
            if (Math.Abs(Blocks[id].Hardness - -1.0F) < 0.001F)
            {
                return false;
            }

            if (Blocks[id].GetPistonBehavior() == PistonBehavior.Unpushable)
            {
                return false;
            }

            if (!allowBreaking && Blocks[id].GetPistonBehavior() == PistonBehavior.Break)
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

    private static bool CanExtend(IWorldContext ctx, int x, int y, int z, int dir)
    {
        int checkX = x + PistonConstants.HeadOffsetX[dir];
        int checkY = y + PistonConstants.HeadOffsetY[dir];
        int checkZ = z + PistonConstants.HeadOffsetZ[dir];
        int pushCount = 0;

        while (true)
        {
            if (pushCount >= 13)
            {
                return true;
            }

            if (checkY <= 0 || checkY >= ChuckFormat.WorldHeight - 1) return false;

            int blockId = ctx.Reader.GetBlockId(checkX, checkY, checkZ);
            if (blockId == 0)
            {
                return true;
            }

            if (!CanMoveBlock(blockId, ctx, checkX, checkY, checkZ, true))
            {
                return false;
            }

            if (Blocks[blockId].GetPistonBehavior() == PistonBehavior.Break)
            {
                return true;
            }

            if (pushCount == MaxPushCount)
            {
                return false;
            }

            checkX += PistonConstants.HeadOffsetX[dir];
            checkY += PistonConstants.HeadOffsetY[dir];
            checkZ += PistonConstants.HeadOffsetZ[dir];
            ++pushCount;
        }
    }

    private bool Push(IWorldContext ctx, int x, int y, int z, int dir)
    {
        int nextX = x + PistonConstants.HeadOffsetX[dir];
        int nextY = y + PistonConstants.HeadOffsetY[dir];
        int nextZ = z + PistonConstants.HeadOffsetZ[dir];
        int pushCount = 0;

        while (true)
        {
            if (pushCount < 13)
            {
                if (nextY <= 0 || nextY >= ChuckFormat.WorldHeight - 1) return false;

                int blockId = ctx.Reader.GetBlockId(nextX, nextY, nextZ);
                if (blockId != 0)
                {
                    if (!CanMoveBlock(blockId, ctx, nextX, nextY, nextZ, true))
                    {
                        return false;
                    }

                    if (Blocks[blockId].GetPistonBehavior() != PistonBehavior.Break)
                    {
                        if (pushCount == 12)
                        {
                            return false;
                        }

                        nextX += PistonConstants.HeadOffsetX[dir];
                        nextY += PistonConstants.HeadOffsetY[dir];
                        nextZ += PistonConstants.HeadOffsetZ[dir];
                        ++pushCount;
                        continue;
                    }

                    Blocks[blockId].DropStacks(new OnDropEvent(ctx, nextX, nextY, nextZ, ctx.Reader.GetBlockMeta(nextX, nextY, nextZ)));
                    ctx.Writer.SetBlock(nextX, nextY, nextZ, 0);
                }
            }

            while (nextX != x || nextY != y || nextZ != z)
            {
                int prevX = nextX - PistonConstants.HeadOffsetX[dir];
                int prevY = nextY - PistonConstants.HeadOffsetY[dir];
                int prevZ = nextZ - PistonConstants.HeadOffsetZ[dir];

                int prevBlockId = ctx.Reader.GetBlockId(prevX, prevY, prevZ);
                int prevMeta = ctx.Reader.GetBlockMeta(prevX, prevY, prevZ);

                if (prevBlockId == ID && prevX == x && prevY == y && prevZ == z)
                {
                    ctx.Writer.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.ID, dir | (_sticky ? 8 : 0));
                    ctx.Entities.SetBlockEntity(nextX, nextY, nextZ, BlockPistonMoving.CreatePistonBlockEntity(PistonHead.ID, dir | (_sticky ? 8 : 0), dir, true, false));
                }
                else
                {
                    ctx.Writer.SetBlockWithoutNotifyingNeighbors(nextX, nextY, nextZ, MovingPiston.ID, prevMeta);
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
