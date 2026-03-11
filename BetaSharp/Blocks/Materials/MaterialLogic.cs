using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials
{
    internal class MaterialLogic : Material
    {
        public override bool IsSolid => false;
        public override bool BlocksVision => false;
        public override bool BlocksMovement => false;

        public MaterialLogic(MapColor mapColor) : base(mapColor)
        {
        }

    }

}
