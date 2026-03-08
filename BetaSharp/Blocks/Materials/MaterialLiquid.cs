namespace BetaSharp.Blocks.Materials;

internal class MaterialLiquid : Material
{
    public MaterialLiquid(MapColor mapColor) : base(mapColor)
    {
        SetReplaceable();
        SetDestroyPistonBehavior();
    }

    public override bool IsFluid => true;
    public override bool IsSolid => false;
    public override bool BlocksMovement => false;
}
