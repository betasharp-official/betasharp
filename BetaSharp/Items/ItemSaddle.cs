using BetaSharp.Entities;

namespace BetaSharp.Items;

internal class ItemSaddle : Item
{
    public ItemSaddle(int id) : base(id) => MaxCount = 1;

    public override void UseOnEntity(ItemStack itemStack, EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
        if (entityLiving is not EntityPig pig)
        {
            return;
        }

        if (pig.Saddled.Value)
        {
            return;
        }

        pig.Saddled.Value = true;
        itemStack.ConsumeItem(entityPlayer);
    }

    public override bool PostHit(ItemStack itemStack, EntityLiving a, EntityPlayer b)
    {
        UseOnEntity(itemStack, a, b);
        return true;
    }
}
