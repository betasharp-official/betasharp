namespace BetaSharp;

internal class ToolMaterial
{
    public static readonly ToolMaterial WOOD = new(0, 59, 2.0F, 0);
    public static readonly ToolMaterial STONE = new(1, 131, 4.0F, 1);
    public static readonly ToolMaterial IRON = new(2, 250, 6.0F, 2);
    public static readonly ToolMaterial EMERALD = new(3, 1561, 8.0F, 3);
    public static readonly ToolMaterial GOLD = new(0, 32, 12.0F, 0);

    private readonly int harvestLevel;
    private readonly int maxUses;
    private readonly float efficiencyOnProperMaterial;
    private readonly int damageVsEntity;

    private ToolMaterial(int harvestLevel, int maxUses, float efficiencyOnProperMaterial, int damageVsEntity)
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
