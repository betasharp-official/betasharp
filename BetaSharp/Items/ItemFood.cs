using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemFood : Item
{

    private int healAmount;
    private bool isWolfsFavoriteMeat;

    public ItemFood(int id, int healAmount, bool isWolfsFavoriteMeat) : base(id)
    {
        this.healAmount = healAmount;
        this.isWolfsFavoriteMeat = isWolfsFavoriteMeat;
        maxCount = 1;
    }

    public override ItemStack use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        itemStack.ConsumeItem(entityPlayer);
        entityPlayer.heal(healAmount);
        return itemStack;
    }

    public int getHealAmount()
    {
        return healAmount;
    }

    public bool getIsWolfsFavoriteMeat()
    {
        return isWolfsFavoriteMeat;
    }
}
