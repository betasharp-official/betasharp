using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockNote : BlockWithEntity
{
    public BlockNote(int id) : base(id, 74, Material.Wood)
    {
    }

    public override int getTexture(int side) => textureId;

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!(@event.BlockId > 0 && Blocks[@event.BlockId].canEmitRedstonePower()))
        {
            return;
        }

        bool isPowered = @event.World.Redstone.IsStrongPowered(@event.X, @event.Y, @event.Z);
        BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
        if (blockEntity != null && blockEntity.powered != isPowered)
        {
            if (isPowered)
            {
                blockEntity.playNote(@event.World, @event.X, @event.Y, @event.Z);
            }

            blockEntity.powered = isPowered;
        }
    }

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return true;
        }

        BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
        if (blockEntity == null)
        {
            return false;
        }

        blockEntity.cycleNote();
        blockEntity.playNote(@event.World, @event.X, @event.Y, @event.Z);
        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
            blockEntity?.playNote(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    public override BlockEntity getBlockEntity() => new BlockEntityNote();

    public override void onBlockAction(OnBlockActionEvent @event)
    {
        float pitch = (float)Math.Pow(2.0D, (@event.Data2 - 12) / 12.0D);
        string instrumentName = "harp";
        if (@event.Data1 == 1)
        {
            instrumentName = "bd";
        }

        if (@event.Data1 == 2)
        {
            instrumentName = "snare";
        }

        if (@event.Data1 == 3)
        {
            instrumentName = "hat";
        }

        if (@event.Data1 == 4)
        {
            instrumentName = "bassattack";
        }

        @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "note." + instrumentName, 3.0F, pitch);
        @event.World.Broadcaster.AddParticle("note", @event.X + 0.5D, @event.Y + 1.2D, @event.Z + 0.5D, @event.Data2 / 24.0D, 0.0D, 0.0D);
    }
}
