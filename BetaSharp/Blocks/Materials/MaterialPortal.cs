namespace BetaSharp.Blocks.Materials;

internal class MaterialPortal : Material
{
    public MaterialPortal(MapColor mapColor) : base(mapColor)
    {
    }

    public override bool IsSolid => false;
    public override bool BlocksVision => false;
    public override bool BlocksMovement => false;
}
