namespace BetaSharp;

internal class EnumToolMaterial
{
    public static readonly EnumToolMaterial WOOD = new(0, 59, 2.0F, 0);
    public static readonly EnumToolMaterial STONE = new(1, 131, 4.0F, 1);
    public static readonly EnumToolMaterial IRON = new(2, 250, 6.0F, 2);
    public static readonly EnumToolMaterial EMERALD = new(3, 1561, 8.0F, 3);
    public static readonly EnumToolMaterial GOLD = new(0, 32, 12.0F, 0);

    private readonly int harvestLevel;
    private readonly int maxUses;
    private readonly float efficiencyOnProperMaterial;
    private readonly int damageVsEntity;

    private EnumToolMaterial(int harvestLevel, int maxUses, float efficiencyOnProperMaterial, int damageVsEntity)
    {
        this.harvestLevel = harvestLevel;
        this.maxUses = maxUses;
        this.efficiencyOnProperMaterial = efficiencyOnProperMaterial;
        this.damageVsEntity = damageVsEntity;
    }

    public int getMaxUses()
    {
        return maxUses;
    }

    public float getEfficiencyOnProperMaterial()
    {
        return efficiencyOnProperMaterial;
    }

    public int getDamageVsEntity()
    {
        return damageVsEntity;
    }

    public int getHarvestLevel()
    {
        return harvestLevel;
    }
}
