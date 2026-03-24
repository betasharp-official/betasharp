using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityCow : EntityAnimal
{
    public override EntityType Type => EntityRegistry.Cow;
    
    public EntityCow(IWorldContext world) : base(world)
    {
        this.texture = "/mob/cow.png";
        this.setBoundingBoxSpacing(0.9F, 1.3F);
    }

    protected override string getLivingSound()
    {
        return "mob.cow";
    }

    protected override string getHurtSound()
    {
        return "mob.cowhurt";
    }

    protected override string getDeathSound()
    {
        return "mob.cowhurt";
    }

    protected override float getSoundVolume()
    {
        return 0.4F;
    }

    protected override int getDropItemId()
    {
        return Item.Leather.id;
    }

    public override bool interact(EntityPlayer player)
    {
        ItemStack heldBucket = player.inventory.getSelectedItem();
        if (heldBucket != null && heldBucket.itemId == Item.Bucket.id)
        {
            player.inventory.setStack(player.inventory.selectedSlot, new ItemStack(Item.MilkBucket));
            return true;
        }
        else
        {
            return false;
        }
    }
}
