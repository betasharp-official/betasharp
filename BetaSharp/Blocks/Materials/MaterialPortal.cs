using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials;

/// <summary>
///     Material for portals, which are non-solid and do not block vision or movement. This is used for nether portals.
/// </summary>
internal class MaterialPortal : Material
{
    public MaterialPortal(MapColor mapColor) : base(mapColor)
    {
    }

    public override bool IsSolid => false;
    public override bool BlocksVision => false;
    public override bool BlocksMovement => false;
}
