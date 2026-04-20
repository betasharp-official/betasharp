using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks.Entities;

/// <summary>
///     Block entity for both the extending and source blocks of a piston.
/// </summary>
public class BlockEntityPiston : BlockEntity
{
    private float _lastProgress;
    private float _progress;

    public BlockEntityPiston()
    {
    }

    public BlockEntityPiston(int pushedBlockId, int pushedBlockData, int facing, bool extending, bool source)
    {
        PushedBlockId = pushedBlockId;
        PushedBlockData = pushedBlockData;
        Facing = facing;
        IsExtending = extending;
        IsSource = source;
    }

    public override BlockEntityType Type => Piston;

    public int PushedBlockId { get; private set; }
    public new int PushedBlockData { get; private set; }
    public bool IsExtending { get; private set; }
    public int Facing { get; private set; }
    public bool IsSource { get; }
    public bool IsExtensionIncomplete => IsExtending && _progress < 1.0F;

    public float GetProgress(float tickDelta)
    {
        if (tickDelta > 1.0F)
        {
            tickDelta = 1.0F;
        }

        return _progress + (_lastProgress - _progress) * tickDelta;
    }

    public float GetRenderOffsetX(float tickDelta) => IsExtending ? (GetProgress(tickDelta) - 1.0F) * PistonConstants.HeadOffsetX[Facing] : (1.0F - GetProgress(tickDelta)) * PistonConstants.HeadOffsetX[Facing];

    public float GetRenderOffsetY(float tickDelta) => IsExtending ? (GetProgress(tickDelta) - 1.0F) * PistonConstants.HeadOffsetY[Facing] : (1.0F - GetProgress(tickDelta)) * PistonConstants.HeadOffsetY[Facing];

    public float GetRenderOffsetZ(float tickDelta) => IsExtending ? (GetProgress(tickDelta) - 1.0F) * PistonConstants.HeadOffsetZ[Facing] : (1.0F - GetProgress(tickDelta)) * PistonConstants.HeadOffsetZ[Facing];

    private void PushEntities(EntityManager entities, float collisionShapeSizeMultiplier, float entityMoveMultiplier)
    {
        if (!IsExtending)
        {
            --collisionShapeSizeMultiplier;
        }
        else
        {
            collisionShapeSizeMultiplier = 1.0F - collisionShapeSizeMultiplier;
        }

        Box? pushCollisionBox = Block.MovingPiston.GetPushedBlockCollisionShape(World.Reader, entities, X, Y, Z, PushedBlockId, collisionShapeSizeMultiplier, Facing);
        if (pushCollisionBox == null)
        {
            return;
        }

        List<Entity> entitiesToPush = World.Entities.GetEntities(null!, pushCollisionBox.Value);
        if (entitiesToPush.Count <= 0)
        {
            return;
        }

        List<Entity> pushedEntities = [];
        pushedEntities.AddRange(entitiesToPush);
        foreach (Entity entity in pushedEntities)
        {
            entity.Move(
                entityMoveMultiplier * PistonConstants.HeadOffsetX[Facing],
                entityMoveMultiplier * PistonConstants.HeadOffsetY[Facing],
                entityMoveMultiplier * PistonConstants.HeadOffsetZ[Facing]
            );
        }

        pushedEntities.Clear();
    }

    private void FinalizeBlock()
    {
        if (World.Reader.GetBlockId(X, Y, Z) == Block.MovingPiston.ID)
        {
            World.Writer.SetBlock(X, Y, Z, PushedBlockId, PushedBlockData);
            if (!World.IsRemote)
            {
                World.Broadcaster.NotifyNeighbors(X, Y, Z, PushedBlockId);
                World.Broadcaster.BlockUpdateEvent(X, Y, Z);

                if (PushedBlockId == Block.Piston.ID || PushedBlockId == Block.StickyPiston.ID)
                {
                    World.TickScheduler.ScheduleBlockUpdate(X, Y, Z, PushedBlockId, 1);
                }
            }
        }

        World.Entities.RemoveBlockEntity(X, Y, Z);
        MarkRemoved();
    }

    public void AbandonExtensionToStaticBlock() => FinalizeBlock();

    public void Finish()
    {
        if (!(_progress < 1.0F))
        {
            return;
        }

        _progress = _lastProgress = 1.0F;
        FinalizeBlock();
    }

    public override void Tick(EntityManager entities)
    {
        _progress = _lastProgress;
        if (_progress >= 1.0F)
        {
            PushEntities(entities, 1.0F, 0.25F);
            FinalizeBlock();
        }
        else
        {
            _lastProgress += 0.5F;
            if (_lastProgress >= 1.0F)
            {
                _lastProgress = 1.0F;
            }

            if (IsExtending)
            {
                PushEntities(entities, _lastProgress, _lastProgress - _progress + 1.0F / 16.0F);
            }
        }
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        PushedBlockId = nbt.GetInteger("blockId");
        PushedBlockData = nbt.GetInteger("blockData");
        Facing = nbt.GetInteger("facing");
        _progress = _lastProgress = nbt.GetFloat("progress");
        IsExtending = nbt.GetBoolean("extending");
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetInteger("blockId", PushedBlockId);
        nbt.SetInteger("blockData", PushedBlockData);
        nbt.SetInteger("facing", Facing);
        nbt.SetFloat("progress", _progress);
        nbt.SetBoolean("extending", IsExtending);
    }
}
