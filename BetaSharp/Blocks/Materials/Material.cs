namespace BetaSharp.Blocks.Materials;

public class Material
{
    public static readonly Material Air = new MaterialTransparent(MapColor.airColor);
    public static readonly Material SolidOrganic = new(MapColor.grassColor);
    public static readonly Material Soil = new(MapColor.dirtColor);
    public static readonly Material Wood = new Material(MapColor.woodColor).SetBurning();
    public static readonly Material Stone = new Material(MapColor.stoneColor).SetRequiresTool();
    public static readonly Material Metal = new Material(MapColor.ironColor).SetRequiresTool();
    public static readonly Material Water = new MaterialLiquid(MapColor.waterColor).SetDestroyPistonBehavior();
    public static readonly Material Lava = new MaterialLiquid(MapColor.tntColor).SetDestroyPistonBehavior();
    public static readonly Material Leaves = new Material(MapColor.foliageColor).SetBurning().SetTransparent().SetDestroyPistonBehavior();
    public static readonly Material Plant = new MaterialLogic(MapColor.foliageColor).SetDestroyPistonBehavior();
    public static readonly Material Sponge = new(MapColor.clothColor);
    public static readonly Material Wool = new Material(MapColor.clothColor).SetBurning();
    public static readonly Material Fire = new MaterialTransparent(MapColor.airColor).SetDestroyPistonBehavior();
    public static readonly Material Sand = new(MapColor.sandColor);
    public static readonly Material PistonBreakable = new MaterialLogic(MapColor.airColor).SetDestroyPistonBehavior();
    public static readonly Material Glass = new Material(MapColor.airColor).SetTransparent();
    public static readonly Material Tnt = new Material(MapColor.tntColor).SetBurning().SetTransparent();
    public static readonly Material Foliage = new Material(MapColor.foliageColor).SetDestroyPistonBehavior();
    public static readonly Material Ice = new Material(MapColor.iceColor).SetTransparent();
    public static readonly Material SnowLayer = new MaterialLogic(MapColor.snowColor).SetReplaceable().SetTransparent().SetRequiresTool().SetDestroyPistonBehavior();
    public static readonly Material SnowBlock = new Material(MapColor.snowColor).SetRequiresTool();
    public static readonly Material Cactus = new Material(MapColor.foliageColor).SetTransparent().SetDestroyPistonBehavior();
    public static readonly Material Clay = new(MapColor.clayColor);
    public static readonly Material Pumpkin = new Material(MapColor.foliageColor).SetDestroyPistonBehavior();
    public static readonly Material NetherPortal = new MaterialPortal(MapColor.airColor).SetUnpushablePistonBehavior();
    public static readonly Material Cake = new Material(MapColor.airColor).SetDestroyPistonBehavior();
    public static readonly Material Cobweb = new Material(MapColor.clothColor).SetRequiresTool().SetDestroyPistonBehavior();
    public static readonly Material Piston = new Material(MapColor.stoneColor).SetUnpushablePistonBehavior();

    private bool _transparent;
    public Material(MapColor mapColor) => MapColor = mapColor;

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
