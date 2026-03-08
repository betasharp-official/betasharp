using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks;

internal class BlockJukeBox : BlockWithEntity
{
    private static readonly ILogger<BlockJukeBox> s_logger = BetaSharp.Log.Instance.For<BlockJukeBox>();

    public BlockJukeBox(int id, int textureId) : base(id, textureId, Material.Wood)
    {
    }

    public override int getTexture(int side) => textureId + (side == 1 ? 1 : 0);

    public override bool onUse(OnUseEvt ctx)
    {
        if (ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) == 0)
        {
            return false;
        }

        tryEjectRecord(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.Entities, ctx.IsRemote, ctx.X, ctx.Y, ctx.Z);
        return true;
    }

    public void insertRecord(World world, int x, int y, int z, int id)
    {
        if (!world.isRemote)
        {
            BlockEntityRecordPlayer? jukebox = (BlockEntityRecordPlayer?)world.getBlockEntity(x, y, z);
            if (jukebox == null)
            {
                s_logger.LogWarning("Jukebox at {x}, {y}, {z} is missing a block entity", x, y, z);
                return;
            }

            jukebox.recordId = id;
            jukebox.markDirty();
            world.setBlockMeta(x, y, z, 1);
        }
    }

    public void tryEjectRecord(IBlockReader read, IBlockWrite write, WorldEventBroadcaster broadcaster, EntityManager manager, bool isRemote, int x, int y, int z)
    {
        if (!isRemote)
        {
            BlockEntityRecordPlayer? jukebox = (BlockEntityRecordPlayer?)read.GetBlockEntity(x, y, z);
            int recordId = jukebox?.recordId ?? 0;
            if (recordId != 0)
            {
                broadcaster.WorldEvent(1005, x, y, z, 0);
                broadcaster.PlayStreamingAtPos(null, x, y, z);
                jukebox!.recordId = 0;
                jukebox.markDirty();
                write.SetBlockMeta(x, y, z, 0);
                float spreadFactor = 0.7F;
                double offsetX = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
                double offsetY = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.2D + 0.6D;
                double offsetZ = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
                // TODO: Implement this
                // EntityItem entityItem = new(world, x + offsetX, y + offsetY, z + offsetZ, new ItemStack(recordId, 1, 0));
                // entityItem.delayBeforeCanPickup = 10;
                // manager.SpawnEntity(entityItem);
            }
        }
    }

    public override void onBreak(OnBreakEvt ctx)
    {
        tryEjectRecord(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.Entities, ctx.IsRemote, ctx.X, ctx.Y, ctx.Z);
        base.onBreak(ctx);
    }

    public override void dropStacks(OnDropEvt ctx)
    {
        if (!ctx.IsRemote)
        {
            base.dropStacks(ctx);
        }
    }

    protected override BlockEntity getBlockEntity() => new BlockEntityRecordPlayer();
}
