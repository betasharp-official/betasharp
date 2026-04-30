using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

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

    public override ItemStack use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        if (entityPlayer.FishHook != null)
        {
            int durabilityLoss = entityPlayer.FishHook.catchFish();
            itemStack.DamageItem(durabilityLoss, entityPlayer);
            entityPlayer.SwingHand();
        }
        else
        {
            world.Broadcaster.PlaySoundAtEntity(entityPlayer, "random.bow", 0.5F, 0.4F / (itemRand.NextFloat() * 0.4F + 0.8F));
            if (!world.IsRemote)
            {
                world.SpawnEntity(new EntityFish(world, entityPlayer));
            }

            entityPlayer.SwingHand();
        }

        return itemStack;
    }
}
