using BetaSharp.Worlds.Maps;

namespace BetaSharp.Blocks.Materials
{
    public class Material
    {
        public static readonly Material Air = new MaterialTransparent(MapColor.Air);
        public static readonly Material SolidOrganic = new(MapColor.Grass);
        public static readonly Material Soil = new(MapColor.Dirt);
        public static readonly Material Wood = new Material(MapColor.Wood).SetBurning();
        public static readonly Material Stone = new Material(MapColor.Stone).SetRequiresTool();
        public static readonly Material Metal = new Material(MapColor.Iron).SetRequiresTool();
        public static readonly Material Water = new MaterialLiquid(MapColor.Water).SetDestroyPistonBehavior();
        public static readonly Material Lava = new MaterialLiquid(MapColor.TNT).SetDestroyPistonBehavior();
        public static readonly Material Leaves = new Material(MapColor.Foliage).SetBurning().SetTransparent().SetDestroyPistonBehavior();
        public static readonly Material Plant = new MaterialLogic(MapColor.Foliage).SetDestroyPistonBehavior();
        public static readonly Material Sponge = new(MapColor.Cloth);
        public static readonly Material Wool = new Material(MapColor.Cloth).SetBurning();
        public static readonly Material Fire = new MaterialTransparent(MapColor.Air).SetDestroyPistonBehavior();
        public static readonly Material Sand = new(MapColor.Sand);
        public static readonly Material PistonBreakable = new MaterialLogic(MapColor.Air).SetDestroyPistonBehavior();
        public static readonly Material Glass = new Material(MapColor.Air).SetTransparent();
        public static readonly Material Tnt = new Material(MapColor.TNT).SetBurning().SetTransparent();
        public static readonly Material Foliage = new Material(MapColor.Foliage).SetDestroyPistonBehavior();
        public static readonly Material Ice = new Material(MapColor.Ice).SetTransparent();
        public static readonly Material SnowLayer = new MaterialLogic(MapColor.Snow).SetReplaceable().SetTransparent().SetRequiresTool().SetDestroyPistonBehavior();
        public static readonly Material SnowBlock = new Material(MapColor.Snow).SetRequiresTool();
        public static readonly Material Cactus = new Material(MapColor.Foliage).SetTransparent().SetDestroyPistonBehavior();
        public static readonly Material Clay = new(MapColor.Clay);
        public static readonly Material Pumpkin = new Material(MapColor.Foliage).SetDestroyPistonBehavior();
        public static readonly Material NetherPortal = new MaterialPortal(MapColor.Air).SetUnpushablePistonBehavior();
        public static readonly Material Cake = new Material(MapColor.Air).SetDestroyPistonBehavior();
        public static readonly Material Cobweb = new Material(MapColor.Cloth).SetRequiresTool().SetDestroyPistonBehavior();
        public static readonly Material Piston = new Material(MapColor.Stone).SetUnpushablePistonBehavior();

        private bool _transparent;

        public MapColor MapColor { get; }
        public virtual bool IsFluid => false;
        public virtual bool IsSolid => true;
        public virtual bool BlocksVision => true;
        public virtual bool BlocksMovement => true;
        public bool IsBurnable { get; private set; }

        public bool IsReplaceable { get; private set; }

        public bool IsHandHarvestable { get; private set; } = true;

        public int PistonBehavior { get; private set; }

        public bool Suffocates => _transparent ? false : BlocksMovement;

        public Material(MapColor mapColor)
        {
            MapColor = mapColor;
        }

        private Material SetTransparent()
        {
            _transparent = true;
            return this;
        }

        private Material SetRequiresTool()
        {
            IsHandHarvestable = false;
            return this;
        }

        private Material SetBurning()
        {
            IsBurnable = true;
            return this;
        }

        public Material SetReplaceable()
        {
            IsReplaceable = true;
            return this;
        }

        protected Material SetDestroyPistonBehavior()
        {
            PistonBehavior = 1;
            return this;
        }

        protected Material SetUnpushablePistonBehavior()
        {
            PistonBehavior = 2;
            return this;
        }
    }
}
