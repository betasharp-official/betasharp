using BetaSharp.Entities;

namespace BetaSharp.Items;

internal class ItemSaddle : Item
{

    public ItemSaddle(int id) : base(id)
    {
        maxCount = 1;
    }

    public override void useOnEntity(ItemStack itemStack, EntityLiving entityLiving)
    {
        if (entityLiving is EntityPig)
        {
            EntityPig pig = (EntityPig)entityLiving;
            if (!pig.Saddled.Value)
            {
                pig.Saddled.Value = true;
                --itemStack.count;
            }
        }

    }

    public override bool postHit(ItemStack itemStack, EntityLiving a, EntityLiving b)
    {
        useOnEntity(itemStack, a);
        return true;
    }
}
