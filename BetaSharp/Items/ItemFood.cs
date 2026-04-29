using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemFood : Item
{
    private readonly int _healAmount;
    private readonly bool _isWolfsFavoriteMeat;

    public ItemFood(int id, int healAmount, bool isWolfsFavoriteMeat) : base(id)
    {
        _healAmount = healAmount;
        _isWolfsFavoriteMeat = isWolfsFavoriteMeat;
        MaxCount = 1;
    }

    public override ItemStack Use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        itemStack.ConsumeItem(entityPlayer);
        entityPlayer.heal(_healAmount);
        return itemStack;
    }

    public int getHealAmount() => _healAmount;

    public bool getIsWolfsFavoriteMeat() => _isWolfsFavoriteMeat;
}
