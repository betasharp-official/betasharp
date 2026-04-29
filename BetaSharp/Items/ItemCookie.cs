namespace BetaSharp.Items;

internal class ItemCookie : ItemFood
{
    public ItemCookie(int id, int healAmount, bool isWolfsFavoriteMeat, int maxCount) : base(id, healAmount, isWolfsFavoriteMeat) => MaxCount = maxCount;
}
