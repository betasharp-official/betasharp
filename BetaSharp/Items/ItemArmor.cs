namespace BetaSharp.Items;

public class ItemArmor : Item
{
    private static readonly int[] s_damageReduceAmountArray = [3, 8, 6, 3];
    private static readonly int[] s_maxDamageArray = [11, 16, 15, 13];
    public readonly int ArmorLevel;
    public readonly int ArmorType;
    public readonly int DamageReduceAmount;
    public readonly int RenderIndex;

    public ItemArmor(int id, int armorLevel, int renderIndex, int armorType) : base(id)
    {
        ArmorLevel = armorLevel;
        ArmorType = armorType;
        RenderIndex = renderIndex;
        DamageReduceAmount = s_damageReduceAmountArray[armorType];
        SetMaxDamage((s_maxDamageArray[armorType] * 3) << armorLevel);
        MaxCount = 1;
    }
}
