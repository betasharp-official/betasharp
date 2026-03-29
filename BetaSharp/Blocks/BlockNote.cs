using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockNote(int id) : BlockWithEntity(id, 74, Material.Wood)
{
    private static readonly string[] s_instrumentSounds =
    [
        "note.harp",
        "note.bd",
        "note.snare",
        "note.hat",
        "note.bassattack"
    ];

    private static string GetInstrumentName(int data1)
    {
        if (data1 < 0 || data1 >= s_instrumentSounds.Length)
        {
            return s_instrumentSounds[0];
        }

        return s_instrumentSounds[data1];
    }

    public override int GetTexture(Side side) => TextureId;

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!(@event.BlockId > 0 && Blocks[@event.BlockId]!.CanEmitRedstonePower)) return;

        bool isPowered = @event.World.Redstone.IsStrongPowered(@event.X, @event.Y, @event.Z);
        BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
        if (blockEntity == null || blockEntity.Powered == isPowered) return;

        if (isPowered)
        {
            blockEntity.PlayNote(@event.World, @event.X, @event.Y, @event.Z);
        }

        blockEntity.Powered = isPowered;
    }

    public override bool OnUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote) return true;

        BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
        if (blockEntity == null) return false;

        blockEntity.CycleNote();
        blockEntity.PlayNote(@event.World, @event.X, @event.Y, @event.Z);
        return true;
    }

    public override void OnBlockBreakStart(OnBlockBreakStartEvent @event)
    {
        if (@event.World.IsRemote) return;

        BlockEntityNote? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntityNote>(@event.X, @event.Y, @event.Z);
        blockEntity?.PlayNote(@event.World, @event.X, @event.Y, @event.Z);
    }

    public override BlockEntity? GetBlockEntity() => new BlockEntityNote();

    public override void OnBlockAction(OnBlockActionEvent @event)
    {
        string soundName = GetInstrumentName(@event.Data1);

        @event.World.Broadcaster.PlaySoundAtPos(
            @event.X + 0.5D,
            @event.Y + 0.5D,
            @event.Z + 0.5D,
            soundName,
            3.0F,
            (float)Math.Pow(2.0, (@event.Data2 - 12.0) / 12.0)
        );
    }
}
