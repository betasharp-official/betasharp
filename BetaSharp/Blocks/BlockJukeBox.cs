using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks;

internal class BlockJukeBox : BlockWithEntity
{
    private static readonly ILogger<BlockJukeBox> s_logger = BetaSharp.Log.Instance.For<BlockJukeBox>();

    public BlockJukeBox(int id, int textureId) : base(id, textureId, Material.Wood)
    {
    }

    public override int getTexture(int side) => textureId + (side == 1 ? 1 : 0);

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) == 0)
        {
            return false;
        }

        tryEjectRecord(@event.World, @event.X, @event.Y, @event.Z);
        return true;
    }

    public void insertRecord(IWorldContext world, int x, int y, int z, int id)
    {
        if (!world.IsRemote)
        {
            BlockEntityRecordPlayer? jukebox = world.Entities.GetBlockEntity<BlockEntityRecordPlayer>(x, y, z);
            if (jukebox == null)
            {
                s_logger.LogWarning("Jukebox at {x}, {y}, {z} is missing a block entity", x, y, z);
                return;
            }

            jukebox.recordId = id;
            jukebox.markDirty();
            world.Writer.SetBlockMeta(x, y, z, 1);
        }
    }

    public void tryEjectRecord(IWorldContext level, int x, int y, int z)
    {
        if (!level.IsRemote)
        {
            BlockEntityRecordPlayer? jukebox = level.Entities.GetBlockEntity<BlockEntityRecordPlayer>(x, y, z);
            int recordId = jukebox?.recordId ?? 0;
            if (recordId != 0)
            {
                level.Broadcaster.WorldEvent(1005, x, y, z, 0);
                level.Broadcaster.PlayStreamingAtPos(null, x, y, z);
                jukebox!.recordId = 0;
                jukebox.markDirty();
                level.Writer.SetBlockMeta(x, y, z, 0);
                float spreadFactor = 0.7F;
                double offsetX = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
                double offsetY = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.2D + 0.6D;
                double offsetZ = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
                EntityItem entityItem = new(level, x + offsetX, y + offsetY, z + offsetZ, new ItemStack(recordId, 1, 0));
                entityItem.delayBeforeCanPickup = 10;
                level.SpawnEntity(entityItem);
            }
        }
    }

    public override void onBreak(OnBreakEvent @event)
    {
        tryEjectRecord(@event.World, @event.X, @event.Y, @event.Z);
        base.onBreak(@event);
    }

    public override void dropStacks(OnDropEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            base.dropStacks(@event);
        }
    }

    public override BlockEntity getBlockEntity() => new BlockEntityRecordPlayer();
}
