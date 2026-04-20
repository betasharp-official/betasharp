using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials;

/// <summary>
///     A liquid material, such as water or lava. Liquids are not solid and do not block movement, but they can still have
///     a map color and be replaced by other blocks.
/// </summary>
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
