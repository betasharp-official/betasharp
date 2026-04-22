using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockNote(int id) : BlockWithEntity(id, BlockTextures.NoteBlock, Material.Wood)
{
    public override int GetTexture(Side side) => BlockTextures.NoteBlock;

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!(@event.BlockId > 0 && Blocks[@event.BlockId].canEmitRedstonePower())) return;

        bool isPowered = @event.World.Redstone.IsStrongPowered(@event.X, @event.Y, @event.Z);
        BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
        if (blockEntity == null || blockEntity.powered == isPowered) return;

        if (isPowered)
        {
            blockEntity.playNote(@event.World, @event.X, @event.Y, @event.Z);
        }

        blockEntity.powered = isPowered;
    }

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote) return true;

        BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
        if (blockEntity == null) return false;

        blockEntity.cycleNote();
        blockEntity.playNote(@event.World, @event.X, @event.Y, @event.Z);
        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent @event)
    {
        if (@event.World.IsRemote) return;

        BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
        blockEntity?.playNote(@event.World, @event.X, @event.Y, @event.Z);
    }

    public override BlockEntity getBlockEntity() => new BlockEntityNote();

    public override void onBlockAction(OnBlockActionEvent @event)
    {
        float pitch = (float)Math.Pow(2.0D, (@event.Data2 - 12) / 12.0D);
        string instrumentName = @event.Data1 switch
        {
            1 => "bd",
            2 => "snare",
            3 => "hat",
            4 => "bassattack",
            _ => "harp"
        };

        @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "note." + instrumentName, 3.0F, pitch);
        @event.World.Broadcaster.AddParticle("note", @event.X + 0.5D, @event.Y + 1.2D, @event.Z + 0.5D, @event.Data2 / 24.0D, 0.0D, 0.0D);
    }
}
