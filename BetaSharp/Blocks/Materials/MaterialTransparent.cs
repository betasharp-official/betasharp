using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials;

/// <summary>
///     Transparent material, not solid and not blocking vision or movement. Examples include air and fire.
/// </summary>
internal class MaterialTransparent : Material
{
    public MaterialTransparent(MapColor mapColor) : base(mapColor) => SetReplaceable();
    public override bool IsSolid => false;

    public override bool BlocksVision => false;

    public override bool BlocksMovement => false;
}
