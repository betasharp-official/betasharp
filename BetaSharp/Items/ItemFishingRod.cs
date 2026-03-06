using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemFishingRod : Item
{

    public ItemFishingRod(int id) : base(id)
    {
        setMaxDamage(64);
        setMaxCount(1);
    }

    public override bool isHandheld()
    {
        return true;
    }

    public override bool isHandheldRod()
    {
        return true;
    }

    public override ItemStack use(ItemStack itemStack, World world, EntityPlayer entityPlayer)
    {
        if (entityPlayer.fishHook != null)
        {
            int durabilityLoss = entityPlayer.fishHook.catchFish();
            itemStack.damageItem(durabilityLoss, entityPlayer);
            entityPlayer.swingHand();
        }
        else
        {
            world.playSound(entityPlayer, "random.bow", 0.5F, 0.4F / (itemRand.NextFloat() * 0.4F + 0.8F));
            if (!world.IsRemote)
            {
                world.Entities.SpawnEntity(new EntityFish(world, entityPlayer));
            }

            entityPlayer.swingHand();
        }

        return itemStack;
    }
}
