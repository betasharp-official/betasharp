using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemBow : Item
{
    public ItemBow(int id) : base(id) => MaxCount = 1;

    public override ItemStack Use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        if (!entityPlayer.inventory.ConsumeInventoryItem(ARROW.Id))
        {
            return itemStack;
        }

        world.Broadcaster.PlaySoundAtEntity(entityPlayer, "random.bow", 1.0F, 1.0F / (itemRand.NextFloat() * 0.4F + 0.8F));
        if (!world.IsRemote)
        {
            world.SpawnEntity(new EntityArrow(world, entityPlayer));
        }

        return itemStack;
    }
}
