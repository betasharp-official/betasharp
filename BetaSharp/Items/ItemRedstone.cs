using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemRedstone(int id) : Item(id)
{
    public override bool UseOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (world.Reader.GetBlockId(x, y, z) != Block.Snow.id)
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

            if (!world.Reader.IsAir(x, y, z))
            {
                return false;
            }
        }

        if (!Block.RedstoneWire.canPlaceAt(new CanPlaceAtContext(world, 0, x, y, z)))
        {
            return true;
        }

        itemStack.ConsumeItem(entityPlayer);
        world.Writer.SetBlock(x, y, z, Block.RedstoneWire.id);

        return true;
    }
}
