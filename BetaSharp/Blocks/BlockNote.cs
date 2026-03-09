using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
namespace BetaSharp.Blocks;

internal class BlockNote : BlockWithEntity
{
    public BlockNote(int id) : base(id, 74, Material.Wood)
    {
    }

    public override int getTexture(int side) => textureId;

    public override void neighborUpdate(OnTickEvt evt)
    {
        if (!(evt.BlockId > 0 && Blocks[evt.BlockId].canEmitRedstonePower())) return;
        
            bool isPowered = evt.Level.Redstone.IsStrongPowered(evt.X, evt.Y, evt.Z);
            BlockEntityNote? blockEntity = (BlockEntityNote?)evt.Level.Entities.GetBlockEntity(evt.X, evt.Y, evt.Z);
            if (blockEntity != null && blockEntity.powered != isPowered)
            {
                if (isPowered)
                {
                    blockEntity.playNote(evt.Level, evt.X, evt.Y, evt.Z);
                }

                blockEntity.powered = isPowered;
            }
        
    }

    public override bool onUse(OnUseEvt evt)
    {
        if (evt.Level.IsRemote)
        {
            return true;
        }

        BlockEntityNote? blockEntity = (BlockEntityNote?)evt.Level.Entities.GetBlockEntity(evt.X, evt.Y, evt.Z);
        if(blockEntity == null) return false;
        blockEntity.cycleNote();
        blockEntity.playNote(evt.Level, evt.X, evt.Y, evt.Z);
        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvt evt)
    {
        if (!evt.Level.IsRemote)
        {
            BlockEntityNote? blockEntity = (BlockEntityNote?)evt.Level.Entities.GetBlockEntity(evt.X, evt.Y, evt.Z);
            blockEntity?.playNote(evt.Level, evt.X, evt.Y, evt.Z);
        }
    }

    protected override BlockEntity getBlockEntity() => new BlockEntityNote();

    public override void onBlockAction(OnBlockActionEvt evt)
    {
        float pitch = (float)Math.Pow(2.0D, (evt.Data2 - 12) / 12.0D);
        string instrumentName = "harp";
        if (evt.Data1 == 1)
        {
            instrumentName = "bd";
        }

        if (evt.Data1 == 2)
        {
            instrumentName = "snare";
        }

        if (evt.Data1 == 3)
        {
            instrumentName = "hat";
        }

        if (evt.Data1 == 4)
        {
            instrumentName = "bassattack";
        }

        evt.Level.Broadcaster.PlaySoundAtPos(evt.X + 0.5D, evt.Y + 0.5D, evt.Z + 0.5D, "note." + instrumentName, 3.0F, pitch);
        evt.Level.Broadcaster.AddParticle("note", evt.X + 0.5D, evt.Y + 1.2D, evt.Z + 0.5D, evt.Data2 / 24.0D, 0.0D, 0.0D);
    }
}
