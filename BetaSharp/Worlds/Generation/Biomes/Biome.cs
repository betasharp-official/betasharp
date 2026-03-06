using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Generation.Generators.Features;
using java.awt;

namespace BetaSharp.Worlds.Generation.Biomes;

public class Biome
{
    public static readonly Biome Rainforest = new BiomeGenRainforest().SetColor(0x8FA360).SetName("Rainforest").SetFoliageColor(0x1FF458);
    public static readonly Biome Swampland = new BiomeGenSwamp().SetColor(0x7F9B20).SetName("Swampland").SetFoliageColor(0x8BAF48);
    public static readonly Biome SeasonalForest = new Biome().SetColor(0x9BE023).SetName("Seasonal Forest");
    public static readonly Biome Forest = new BiomeGenForest().SetColor(0x566210).SetName("Forest").SetFoliageColor(0x4EBA31);
    public static readonly Biome Savanna = new BiomeGenDesert().SetColor(0xD9E023).SetName("Savanna");
    public static readonly Biome Shrubland = new Biome().SetColor(0xA1AD20).SetName("Shrubland");
    public static readonly Biome Taiga = new BiomeGenTaiga().SetColor(0x2EB153).SetName("Taiga").EnableSnow().SetFoliageColor(0x7BB731);
    public static readonly Biome Desert = new BiomeGenDesert().SetColor(0xFA9418).SetName("Desert").DisableRain();
    public static readonly Biome Plains = new BiomeGenDesert().SetColor(0xFFD910).SetName("Plains");
    public static readonly Biome IceDesert = new BiomeGenDesert().SetColor(0xFFED93).SetName("Ice Desert").EnableSnow().DisableRain().SetFoliageColor(0xC4D339);
    public static readonly Biome Tundra = new Biome().SetColor(0x57EBF9).SetName("Tundra").EnableSnow().SetFoliageColor(0xC4D339);
    public static readonly Biome Hell = new BiomeGenHell().SetColor(0xFF0000).SetName("Hell").DisableRain();
    public static readonly Biome Sky = new BiomeGenSky().SetColor(0x8080FF).SetName("Sky").DisableRain();

    private static readonly Biome[] Biomes = new Biome[4096];
    public byte SoilBlockId = (byte)Block.Dirt.id;
    public byte TopBlockId = (byte)Block.GrassBlock.id;

    static Biome() => Init();

    protected Biome()
    {
        MonsterList.Add(new SpawnListEntry(w => new EntitySpider(w)), 10);
        MonsterList.Add(new SpawnListEntry(w => new EntityZombie(w)), 10);
        MonsterList.Add(new SpawnListEntry(w => new EntitySkeleton(w)), 10);
        MonsterList.Add(new SpawnListEntry(w => new EntityCreeper(w)), 10);
        MonsterList.Add(new SpawnListEntry(w => new EntitySlime(w)), 10);

        CreatureList.Add(new SpawnListEntry(w => new EntitySheep(w)), 12);
        CreatureList.Add(new SpawnListEntry(w => new EntityPig(w)), 10);
        CreatureList.Add(new SpawnListEntry(w => new EntityChicken(w)), 10);
        CreatureList.Add(new SpawnListEntry(w => new EntityCow(w)), 8);

        WaterCreatureList.Add(new SpawnListEntry(w => new EntitySquid(w)), 10);
    }

    public string Name { get; private set; } = "";
    public int GrassColor { get; private set; }
    public int FoliageColor { get; private set; } = 0x4EE031;
    protected WeightedRandomSelector<SpawnListEntry> MonsterList { get; } = new();
    protected WeightedRandomSelector<SpawnListEntry> CreatureList { get; } = new();
    protected WeightedRandomSelector<SpawnListEntry> WaterCreatureList { get; } = new();

    public bool HasSnow { get; private set; }
    public bool HasRain { get; private set; } = true;

    protected Biome DisableRain()
    {
        HasRain = false;
        return this;
    }

    protected Biome EnableSnow()
    {
        HasSnow = true;
        return this;
    }

    protected Biome SetName(string name)
    {
        Name = name;
        return this;
    }

    protected Biome SetFoliageColor(int color)
    {
        FoliageColor = color;
        return this;
    }

    protected Biome SetColor(int color)
    {
        GrassColor = color;
        return this;
    }

    public static void Init()
    {
        for (int i = 0; i < 64; ++i)
        {
            for (int j = 0; j < 64; ++j)
            {
                Biomes[i + j * 64] = LocateBiome(i / 63.0F, j / 63.0F);
            }
        }

        Desert.TopBlockId = Desert.SoilBlockId = (byte)Block.Sand.id;
        IceDesert.TopBlockId = IceDesert.SoilBlockId = (byte)Block.Sand.id;
    }

    public virtual Feature GetRandomWorldGenForTrees(JavaRandom rand) => rand.NextInt(10) == 0 ? new LargeOakTreeFeature() : new OakTreeFeature();


    public static Biome GetBiome(double temp, double downfall)
    {
        int x = (int)(temp * 63.0D);
        int y = (int)(downfall * 63.0D);
        return Biomes[x + y * 64];
    }

    public static Biome LocateBiome(float temperature, float downfall)
    {
        downfall *= temperature;
        if (temperature < 0.1f)
        {
            return Tundra;
        }

        if (downfall < 0.2f)
        {
            if (temperature < 0.5f)
            {
                return Tundra;
            }

            return temperature < 0.95f ? Savanna : Desert;
        }

        if (downfall > 0.5f && temperature < 0.7f)
        {
            return Swampland;
        }

        if (temperature < 0.5f)
        {
            return Taiga;
        }

        if (temperature < 0.97f)
        {
            return downfall < 0.35f ? Shrubland : Forest;
        }

        if (downfall < 0.45f)
        {
            return Plains;
        }

        return downfall < 0.9f ? SeasonalForest : Rainforest;
    }

    public virtual int GetSkyColorByTemp(float var1)
    {
        var1 /= 3.0F;
        if (var1 < -1.0F)
        {
            var1 = -1.0F;
        }

        if (var1 > 1.0F)
        {
            var1 = 1.0F;
        }

        return Color.getHSBColor(224.0F / 360.0F - var1 * 0.05F, 0.5F + var1 * 0.1F, 1.0F).getRGB();
    }

    public WeightedRandomSelector<SpawnListEntry> GetSpawnableList(CreatureKind kind)
    {
        if (kind == CreatureKind.Monster)
        {
            return MonsterList;
        }

        if (kind == CreatureKind.Creature)
        {
            return CreatureList;
        }

        if (kind == CreatureKind.WaterCreature)
        {
            return WaterCreatureList;
        }

        throw new ArgumentException("Invalid creature kind: " + kind);
    }

    public bool GetEnableSnow() => HasSnow;

    public bool CanSpawnLightningBolt() => !HasSnow && HasRain;
}
