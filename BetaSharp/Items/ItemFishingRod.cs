using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemFishingRod : Item
{
    public ItemFishingRod(int id) : base(id)
    {
        SetMaxDamage(64);
        SetMaxCount(1);
    }

    public override bool IsHandheld() => true;

    public override bool IsHandheldRod() => true;

    public override ItemStack Use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        if (entityPlayer.fishHook != null)
        {
            int durabilityLoss = entityPlayer.fishHook.catchFish();
            itemStack.DamageItem(durabilityLoss, entityPlayer);
        }
        else
        {
            world.Broadcaster.PlaySoundAtEntity(entityPlayer, "random.bow", 0.5F, 0.4F / (itemRand.NextFloat() * 0.4F + 0.8F));
            if (!world.IsRemote)
            {
                world.SpawnEntity(new EntityFish(world, entityPlayer));
            }
        }

        entityPlayer.swingHand();

        return itemStack;
    }
}
