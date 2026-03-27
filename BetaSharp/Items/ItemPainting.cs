using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemPainting : Item
{

    public ItemPainting(int id) : base(id)
    {
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (meta == 0)
        {
            return false;
        }
        else if (meta == 1)
        {
            return false;
        }
        else
        {
            byte direction = 0;
            if (meta == 4)
            {
                direction = 1;
            }

            if (meta == 3)
            {
                direction = 2;
            }

            if (meta == 5)
            {
                direction = 3;
            }

            EntityPainting painting = new EntityPainting(world, x, y, z, direction);
            if (painting.CanHangOnWall())
            {
                if (!world.IsRemote)
                {
                    world.SpawnEntity(painting);
                }

                itemStack.ConsumeItem(entityPlayer);
            }

            return true;
        }
    }
}
