using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials;

internal class MaterialPortal(MapColor mapColor) : Material(mapColor)
{
    public override bool IsSolid => false;
    public override bool BlocksVision => false;
    public override bool BlocksMovement => false;
}
