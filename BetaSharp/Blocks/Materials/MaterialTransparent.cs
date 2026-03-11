using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials
{
    internal class MaterialTransparent : Material
    {
        public override bool IsSolid => false;

        public override bool BlocksVision => false;

        public override bool BlocksMovement => false;

        public MaterialTransparent(MapColor mapColor) : base(mapColor)
        {
            SetReplaceable();
        }

    }

}
