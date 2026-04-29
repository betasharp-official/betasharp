using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemSnowball : Item
{
    public ItemSnowball(int id) : base(id) => MaxCount = 16;

    public override ItemStack Use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        itemStack.ConsumeItem(entityPlayer);
        world.Broadcaster.PlaySoundAtEntity(entityPlayer, "random.bow", 0.5F, 0.4F / (itemRand.NextFloat() * 0.4F + 0.8F));
        if (!world.IsRemote)
        {
            world.SpawnEntity(new EntitySnowball(world, entityPlayer));
        }

        return itemStack;
    }
}
