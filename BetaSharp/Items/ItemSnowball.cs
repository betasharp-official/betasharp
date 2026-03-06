using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemSnowball : Item
{

    public ItemSnowball(int id) : base(id)
    {
        maxCount = 16;
    }

    public override ItemStack use(ItemStack itemStack, World world, EntityPlayer entityPlayer)
    {
        --itemStack.count;
        world.playSound(entityPlayer, "random.bow", 0.5F, 0.4F / (itemRand.NextFloat() * 0.4F + 0.8F));
        if (!world.IsRemote)
        {
            world.Entities.SpawnEntity(new EntitySnowball(world, entityPlayer));
        }

        return itemStack;
    }
}
