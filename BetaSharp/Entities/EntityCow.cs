using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds;

namespace BetaSharp.Entities;

public class EntityCow : EntityAnimal
{
    public static readonly new java.lang.Class Class = ikvm.runtime.Util.getClassFromTypeHandle(typeof(EntityCow).TypeHandle);

    public EntityCow(World world) : base(world)
    {
        this.texture = "/mob/cow.png";
        this.setBoundingBoxSpacing(0.9F, 1.3F);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
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

    protected override void dropFewItems()
    {
        dropSomeItems(Item.Leather.id);
        dropSomeItems();
    }

    protected override int getDropItemId()
    {
        return fireTicks > 0 ? Item.CookedPorkchop.id : Item.Beef.id;
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