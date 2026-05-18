using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemReed : Item
{
    private readonly int _blockId;

    public ItemReed(int id, Block block) : base(id)
    {
        _blockId = block.id;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (world.Reader.GetBlockId(x, y, z) == Block.Snow.id)
        {
            meta = 0;
        }
        else
        {
            switch (meta)
            {
                case 0:
                    --y;
                    break;
                case 1:
                    ++y;
                    break;
                case 2:
                    --z;
                    break;
                case 3:
                    ++z;
                    break;
                case 4:
                    --x;
                    break;
                case 5:
                    ++x;
                    break;
            }
        }

        if (itemStack.Count == 0) return false;

        if (Block.Blocks[_blockId].canPlaceAt(new CanPlaceAtContext(world, 0, x, y, z)))
        {
            Block block = Block.Blocks[_blockId];
            if (world.Writer.SetBlock(x, y, z, _blockId))
            {
                Block.Blocks[_blockId].onPlaced(new OnPlacedEvent(world, entityPlayer, meta.ToSide(), meta.ToSide(), x, y, z));
                world.Broadcaster.PlaySoundAtEntity(entityPlayer, block.SoundGroup.StepSound, (block.SoundGroup.Volume + 1.0F) / 2.0F, block.SoundGroup.Pitch * 0.8F);
                itemStack.ConsumeItem(entityPlayer);
            }
        }

        return true;
    }
}
