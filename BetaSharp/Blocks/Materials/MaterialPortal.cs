using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials
{
    internal class MaterialPortal : Material
    {
        public override bool IsSolid => false;
        public override bool BlocksVision => false;
        public override bool BlocksMovement => false;

        public MaterialPortal(MapColor mapColor) : base(mapColor)
        {
        }

    }

}
