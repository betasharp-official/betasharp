using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials;

/// <summary>
///     Logic material, used for blocks that are not solid and do not block vision or movement, for Snow or similar blocks.
/// </summary>
internal class MaterialLogic : Material
{
    public MaterialLogic(MapColor mapColor) : base(mapColor)
    {
    }

    public override bool IsSolid => false;
    public override bool BlocksVision => false;
    public override bool BlocksMovement => false;
}
