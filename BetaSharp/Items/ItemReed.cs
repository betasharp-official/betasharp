using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemReed : Item
{

    private int field_320_a;

    public ItemReed(int id, Block block) : base(id)
    {
        field_320_a = block.id;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (world.Reader.GetBlockId(x, y, z) == Block.Snow.id)
        {
            meta = 0;
        }
        else
        {
            if (meta == 0)
            {
                --y;
            }

            if (meta == 1)
            {
                ++y;
            }

            if (meta == 2)
            {
                --z;
            }

            if (meta == 3)
            {
                ++z;
            }

            if (meta == 4)
            {
                --x;
            }

            if (meta == 5)
            {
                ++x;
            }
        }

        if (itemStack.count == 0)
        {
            return false;
        }
        else
        {
            if (Block.Blocks[field_320_a].canPlaceAt(new CanPlaceAtContext(world, 0, x, y, z)))
            {
                Block block = Block.Blocks[field_320_a];
                if (world.Writer.SetBlock(x, y, z, field_320_a))
                {
                    Block.Blocks[field_320_a].onPlaced(new OnPlacedEvent(world, entityPlayer, meta, meta, x, y, z));
                    world.Broadcaster.PlaySoundAtEntity(entityPlayer, block.soundGroup.StepSound, (block.soundGroup.Volume + 1.0F) / 2.0F, block.soundGroup.Pitch * 0.8F);
                    itemStack.ConsumeItem(entityPlayer);
                }
            }

            return true;
        }
    }
}
