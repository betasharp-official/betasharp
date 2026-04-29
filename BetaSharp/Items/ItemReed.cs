using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemReed(int id, Block block) : Item(id)
{
    private readonly int field_320_a = block.id;

    public override bool UseOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
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

        if (itemStack.Count == 0)
        {
            return false;
        }

        if (!Block.Blocks[field_320_a].canPlaceAt(new CanPlaceAtContext(world, 0, x, y, z)))
        {
            return true;
        }

        Block block = Block.Blocks[field_320_a];
        if (!world.Writer.SetBlock(x, y, z, field_320_a))
        {
            return true;
        }

        Block.Blocks[field_320_a].onPlaced(new OnPlacedEvent(world, entityPlayer, meta.ToSide(), meta.ToSide(), x, y, z));
        world.Broadcaster.PlaySoundAtEntity(entityPlayer, block.SoundGroup.StepSound, (block.SoundGroup.Volume + 1.0F) / 2.0F, block.SoundGroup.Pitch * 0.8F);
        itemStack.ConsumeItem(entityPlayer);

        return true;
    }
}
