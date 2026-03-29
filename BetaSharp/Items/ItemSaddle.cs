using BetaSharp.Entities;

namespace BetaSharp.Items;

internal class ItemSaddle : Item
{

    public ItemSaddle(int id) : base(id)
    {
        maxCount = 1;
    }

    public override void useOnEntity(ItemStack itemStack, EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
        if (entityLiving is EntityPig)
        {
            EntityPig pig = (EntityPig)entityLiving;
            if (!pig.Saddled.Value)
            {
                pig.Saddled.Value = true;
                itemStack.ConsumeItem(entityPlayer);
            }
        }

    }

    public override bool postHit(ItemStack itemStack, EntityLiving a, EntityPlayer b)
    {
        useOnEntity(itemStack, a, b);
        return true;
    }
}
