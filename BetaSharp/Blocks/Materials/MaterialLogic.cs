namespace BetaSharp.Blocks.Materials;

internal class MaterialLogic : Material
{
    public MaterialLogic(MapColor mapColor) : base(mapColor)
    {
    }

    public override bool IsSolid => false;
    public override bool BlocksVision => false;
    public override bool BlocksMovement => false;
}
