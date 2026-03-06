using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemEgg : Item
{

    public ItemEgg(int id) : base(id)
    {
        maxCount = 16;
    }

    public override ItemStack use(ItemStack itemStack, World world, EntityPlayer entityPlayer)
    {
        --itemStack.count;
        world.PlaySound(entityPlayer, "random.bow", 0.5F, 0.4F / (itemRand.NextFloat() * 0.4F + 0.8F));
        if (!world.IsRemote)
        {
            world.Entities.SpawnEntity(new EntityEgg(world, entityPlayer));
        }

        return itemStack;
    }
}
