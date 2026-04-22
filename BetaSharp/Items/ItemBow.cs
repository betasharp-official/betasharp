using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemBow : Item
{

    public ItemBow(int id) : base(id)
    {
        maxCount = 1;
    }

    public override ItemStack use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        if (entityPlayer.inventory.ConsumeInventoryItem(Item.ARROW.id))
        {
            world.Broadcaster.PlaySoundAtEntity(entityPlayer, "random.bow", 1.0F, 1.0F / (itemRand.NextFloat() * 0.4F + 0.8F));
            if (!world.IsRemote)
            {
                world.SpawnEntity(new EntityArrow(world, entityPlayer));
            }
        }

        return itemStack;
    }
}
