using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Registries;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Generation.Generators.Features;

namespace BetaSharp.Worlds.Generation.Biomes;

public class Biome
{
    private static readonly IRegistry<Biome> s_registry = DefaultRegistries.Biomes;

    public static readonly Biome Rainforest = Register(0, "rainforest", new BiomeGenRainforest().SetColor(0x8FA360).SetName("Rainforest").SetFoliageColor(0x1FF458));
    public static readonly Biome Swampland = Register(1, "swampland", new BiomeGenSwamp().SetColor(0x7F9B20).SetName("Swampland").SetFoliageColor(0x8BAF48));
    public static readonly Biome SeasonalForest = Register(2, "seasonal_forest", new Biome().SetColor(0x9BE023).SetName("Seasonal Forest"));
    public static readonly Biome Forest = Register(3, "forest", new BiomeGenForest().SetColor(0x566210).SetName("Forest").SetFoliageColor(0x4EBA31));
    public static readonly Biome Savanna = Register(4, "savanna", new BiomeGenDesert().SetColor(0xD9E023).SetName("Savanna"));
    public static readonly Biome Shrubland = Register(5, "shrubland", new Biome().SetColor(0xA1AD20).SetName("Shrubland"));
    public static readonly Biome Taiga = Register(6, "taiga", new BiomeGenTaiga().SetColor(0x2EB153).SetName("Taiga").EnableSnow().SetFoliageColor(0x7BB731));
    public static readonly Biome Desert = Register(7, "desert", new BiomeGenDesert().SetColor(0xFA9418).SetName("Desert").DisableRain());
    public static readonly Biome Plains = Register(8, "plains", new BiomeGenDesert().SetColor(0xFFD910).SetName("Plains"));
    public static readonly Biome IceDesert = Register(9, "ice_desert", new BiomeGenDesert().SetColor(0xFFED93).SetName("Ice Desert").EnableSnow().DisableRain().SetFoliageColor(0xC4D339));
    public static readonly Biome Tundra = Register(10, "tundra", new Biome().SetColor(0x57EBF9).SetName("Tundra").EnableSnow().SetFoliageColor(0xC4D339));
    public static readonly Biome Hell = Register(11, "hell", new BiomeGenHell().SetColor(0xFF0000).SetName("Hell").DisableRain());
    public static readonly Biome Sky = Register(12, "sky", new BiomeGenSky().SetColor(0x8080FF).SetName("Sky").DisableRain());

    private static readonly Biome[] s_biomes = new Biome[4096];

    public string Name { get; private set; } = "";
    public int GrassColor { get; private set; }
    public byte TopBlockId = (byte)Block.GrassBlock.id;
    public byte SoilBlockId = (byte)Block.Dirt.id;
    public int FoliageColor { get; private set; } = 0x4EE031;
    protected WeightedRandomSelector<SpawnListEntry> MonsterList { get; } = new();
    protected WeightedRandomSelector<SpawnListEntry> CreatureList { get; } = new();
    protected WeightedRandomSelector<SpawnListEntry> WaterCreatureList { get; } = new();

    public bool HasSnow { get; private set; }
    public bool HasRain { get; private set; } = true;

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

    private static Biome Register(int id, string name, Biome biome)
    {
        s_registry.Register(id, ResourceLocation.Parse(name), biome);
        return biome;
    }

    protected Biome DisableRain() { HasRain = false; return this; }
    protected Biome EnableSnow() { HasSnow = true; return this; }
    protected Biome SetName(string name) { Name = name; return this; }
    protected Biome SetFoliageColor(int color) { FoliageColor = color; return this; }
    protected Biome SetColor(int color) { GrassColor = color; return this; }

    public static void Init()
    {
        for (int i = 0; i < 64; ++i)
        {
            for (int j = 0; j < 64; ++j)
            {
                s_biomes[i + j * 64] = LocateBiome(i / 63.0F, j / 63.0F);
            }
        }

        Desert.TopBlockId = Desert.SoilBlockId = (byte)Block.Sand.id;
        IceDesert.TopBlockId = IceDesert.SoilBlockId = (byte)Block.Sand.id;
    }

    public virtual Feature GetRandomWorldGenForTrees(JavaRandom rand)
    {
        return rand.NextInt(10) == 0 ? new LargeOakTreeFeature() : new OakTreeFeature();
    }


    public static Biome GetBiome(double temp, double downfall)
    {
        int x = (int)(temp * 63.0D);
        int y = (int)(downfall * 63.0D);
        return s_biomes[x + y * 64];
    }

    public static Biome LocateBiome(float temperature, float downfall)
    {
        downfall *= temperature;
        if (temperature < 0.1f) return Tundra;
        if (downfall < 0.2f)
        {
            if (temperature < 0.5f) return Tundra;
            return temperature < 0.95f ? Savanna : Desert;
        }
        if (downfall > 0.5f && temperature < 0.7f) return Swampland;
        if (temperature < 0.5f) return Taiga;
        if (temperature < 0.97f) return downfall < 0.35f ? Shrubland : Forest;
        if (downfall < 0.45f) return Plains;
        return downfall < 0.9f ? SeasonalForest : Rainforest;
    }

    public virtual int GetSkyColorByTemp(float temperature)
    {
        temperature /= 3.0F;
        if (temperature < -1.0F)
        {
            temperature = -1.0F;
        }

        if (temperature > 1.0F)
        {
            temperature = 1.0F;
        }

        return ToRgb(224.0f / 360.0f - temperature * 0.05f, 0.5f + temperature * 0.1f, 1.0f);
    }

    public static (int R, int G, int B) FromHsbColor(float hue, float saturation, float brightness)
    {
        if (saturation == 0f)
        {
            int gray = (int)(brightness * 255f + 0.5f);
            return (gray, gray, gray);
        }

        float h = (hue - MathF.Floor(hue)) * 6f;
        float f = h - MathF.Floor(h);
        float p = brightness * (1f - saturation);
        float q = brightness * (1f - saturation * f);
        float t = brightness * (1f - saturation * (1f - f));

        return (int)h switch
        {
            0 => ToRgb(brightness, t, p),
            1 => ToRgb(q, brightness, p),
            2 => ToRgb(p, brightness, t),
            3 => ToRgb(p, q, brightness),
            4 => ToRgb(t, p, brightness),
            _ => ToRgb(brightness, p, q),
        };

        static (int, int, int) ToRgb(float r, float g, float b) =>
            ((int)(r * 255f + 0.5f), (int)(g * 255f + 0.5f), (int)(b * 255f + 0.5f));
    }

    public static int ToRgb(float hue, float saturation, float brightness)
    {
        (int r, int g, int b) = FromHsbColor(hue, saturation, brightness);
        return (255 << 24) | (r << 16) | (g << 8) | b;
    }

    public WeightedRandomSelector<SpawnListEntry> GetSpawnableList(CreatureKind kind)
    {
        if (kind == CreatureKind.Monster) return MonsterList;
        if (kind == CreatureKind.Creature) return CreatureList;
        if (kind == CreatureKind.WaterCreature) return WaterCreatureList;
        throw new ArgumentException("Invalid creature kind: " + kind);
    }

    public bool GetEnableSnow()
    {
        return HasSnow;
    }

    public bool CanSpawnLightningBolt() => !HasSnow && HasRain;

    static Biome() => Init();
}
