namespace BetaSharp.Items;

internal class ItemCoal : Item
{
    public ItemCoal(int id) : base(id)
    {
        SetHasSubtypes(true);
        SetMaxDamage(0);
    }

    public override string GetItemNameIS(ItemStack itemStack) => itemStack.GetDamage() == 1 ? "item.charcoal" : "item.coal";
}
