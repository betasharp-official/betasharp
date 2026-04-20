using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks;

internal class BlockJukeBox(int id, int textureId) : BlockWithEntity(id, textureId, Material.Wood)
{
    private const float DropSpread = 0.7F;
    private static readonly ILogger<BlockJukeBox> s_logger = BetaSharp.Log.Instance.For<BlockJukeBox>();

    public override int GetTexture(Side side) => side switch
    {
        Side.Up => BlockTextures.JukeboxTop,
        _ => BlockTextures.NoteBlock
    };

    public override bool OnUse(OnUseEvent @event)
    {
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) == 0)
        {
            return false;
        }

        TryEjectRecord(@event.World, @event.X, @event.Y, @event.Z);
        return true;
    }

    public static void InsertRecord(IWorldContext world, int x, int y, int z, int id)
    {
        if (world.IsRemote)
        {
            return;
        }

        BlockEntityRecordPlayer? jukebox = world.Entities.GetBlockEntity<BlockEntityRecordPlayer>(x, y, z);
        if (jukebox == null)
        {
            s_logger.LogWarning("Jukebox at {x}, {y}, {z} is missing a block entity", x, y, z);
            return;
        }

        jukebox.RecordID = id;
        jukebox.MarkDirty();
        world.Writer.SetBlockMeta(x, y, z, 1);
    }

    public static void TryEjectRecord(IWorldContext level, int x, int y, int z)
    {
        if (level.IsRemote)
        {
            return;
        }

        BlockEntityRecordPlayer? jukebox = level.Entities.GetBlockEntity<BlockEntityRecordPlayer>(x, y, z);
        int recordId = jukebox?.RecordID ?? 0;
        if (recordId == 0)
        {
            return;
        }

        level.Broadcaster.WorldEvent(1005, x, y, z, 0);
        level.Broadcaster.PlayStreamingAtPos(null, x, y, z);
        jukebox!.RecordID = 0;
        jukebox.MarkDirty();
        level.Writer.SetBlockMeta(x, y, z, 0);

        double offsetX = Random.Shared.NextSingle() * DropSpread + (1.0F - DropSpread) * 0.5D;
        double offsetY = Random.Shared.NextSingle() * DropSpread + (1.0F - DropSpread) * 0.2D + 0.6D;
        double offsetZ = Random.Shared.NextSingle() * DropSpread + (1.0F - DropSpread) * 0.5D;
        EntityItem entityItem = new(level, x + offsetX, y + offsetY, z + offsetZ, new ItemStack(recordId, 1, 0))
        {
            delayBeforeCanPickup = 10
        };
        level.SpawnEntity(entityItem);
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        TryEjectRecord(@event.World, @event.X, @event.Y, @event.Z);
        base.OnBreak(@event);
    }

    public override void DropStacks(OnDropEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            base.DropStacks(@event);
        }
    }

    public override BlockEntity GetBlockEntity() => new BlockEntityRecordPlayer();
}
