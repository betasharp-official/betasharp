namespace BetaSharp.Items;

internal class ItemPiston(int id) : ItemBlock(id)
{
    protected override int GetPlacementMetadata(int meta) => 7;
}
