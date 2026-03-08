namespace BetaSharp.Blocks.Materials;

internal class MaterialTransparent : Material
{
    public MaterialTransparent(MapColor mapColor) : base(mapColor) => SetReplaceable();
    public override bool IsSolid => false;

    public override bool BlocksVision => false;

    public override bool BlocksMovement => false;
}
