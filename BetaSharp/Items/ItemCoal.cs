namespace BetaSharp.Items;

internal class ItemCoal : Item
{

    public ItemCoal(int id) : base(id)
    {
        setHasSubtypes(true);
        setMaxDamage(0);
    }

    public override String getItemNameIS(ItemStack itemStack)
    {
        return itemStack.getDamage() == 1 ? "item.charcoal" : "item.coal";
    }

    public override string[] GetItemAlias() => ["charcoal:1"];
}
