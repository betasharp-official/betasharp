using System.IO.Compression;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace BetaSharp;

public class AssetManager
{
    public enum AssetType
    {
        Binary,
        Text
    }

    public class Asset
    {
        private readonly AssetType type;
        private readonly byte[]? binaryContent;
        private readonly string? textContent;

        public Asset(byte[] binary)
        {
            type = AssetType.Binary;
            binaryContent = binary;
        }

        public Asset(string text)
        {
            type = AssetType.Text;
            textContent = text;
        }

        public AssetType getType() => type;

        public byte[] getBinaryContent()
        {
            if (binaryContent == null || type != AssetType.Binary)
            {
                throw new Exception("Attempted to get binary content from a non binary asset");
            }

            return binaryContent;
        }

        public string getTextContent()
        {
            if (textContent == null || type != AssetType.Text)
            {
                throw new Exception("Attempted to get text content from a non text asset");
            }

            return textContent;
        }
    }

    public static AssetManager Instance { get; } = new();

    private readonly Dictionary<string, AssetType> assetsToLoad = [];
    private readonly Dictionary<string, Asset> loadedAssets = [];
    private readonly HashSet<string> assetDirectories = [];
    private readonly ILogger<AssetManager> _logger = Log.Instance.For<AssetManager>();

    private int embeddedAssetsLoaded;

    private AssetManager()
    {
        defineAsset("title/splashes.txt", AssetType.Text);
        defineAsset("title/black.png", AssetType.Binary);
        defineAsset("title/mclogo.png", AssetType.Binary);
        defineAsset("title/mojang.png", AssetType.Binary);

        defineAsset("font.txt", AssetType.Text);

        defineAsset("achievement/map.txt", AssetType.Text);
        defineAsset("achievement/bg.png", AssetType.Binary);
        defineAsset("achievement/icons.png", AssetType.Binary);

        defineAsset("armor/chain_1.png", AssetType.Binary);
        defineAsset("armor/chain_2.png", AssetType.Binary);
        defineAsset("armor/cloth_1.png", AssetType.Binary);
        defineAsset("armor/cloth_2.png", AssetType.Binary);
        defineAsset("armor/diamond_1.png", AssetType.Binary);
        defineAsset("armor/diamond_2.png", AssetType.Binary);
        defineAsset("armor/gold_1.png", AssetType.Binary);
        defineAsset("armor/gold_2.png", AssetType.Binary);
        defineAsset("armor/iron_1.png", AssetType.Binary);
        defineAsset("armor/iron_2.png", AssetType.Binary);
        defineAsset("armor/power.png", AssetType.Binary);

        defineAsset("art/kz.png", AssetType.Binary);

        defineAsset("environment/clouds.png", AssetType.Binary);
        defineAsset("environment/rain.png", AssetType.Binary);
        defineAsset("environment/snow.png", AssetType.Binary);

        defineAsset("font/default.png", AssetType.Binary);

        defineAsset("gui/background.png", AssetType.Binary);
        defineAsset("gui/container.png", AssetType.Binary);
        defineAsset("gui/crafting.png", AssetType.Binary);
        defineAsset("gui/furnace.png", AssetType.Binary);
        defineAsset("gui/gui.png", AssetType.Binary);
        defineAsset("gui/icons.png", AssetType.Binary);
        defineAsset("gui/inventory.png", AssetType.Binary);
        defineAsset("gui/items.png", AssetType.Binary);
        defineAsset("gui/logo.png", AssetType.Binary);
        defineAsset("gui/particles.png", AssetType.Binary);
        defineAsset("gui/slot.png", AssetType.Binary);
        defineAsset("gui/trap.png", AssetType.Binary);
        defineAsset("gui/unknown_pack.png", AssetType.Binary);

        defineAsset("item/arrows.png", AssetType.Binary);
        defineAsset("item/boat.png", AssetType.Binary);
        defineAsset("item/cart.png", AssetType.Binary);
        defineAsset("item/door.png", AssetType.Binary);
        defineAsset("item/sign.png", AssetType.Binary);

        defineAsset("lang/en_US.lang", AssetType.Text);
        defineAsset("lang/stats_US.lang", AssetType.Text);

        defineAsset("misc/dial.png", AssetType.Binary);
        defineAsset("misc/foliagecolor.png", AssetType.Binary);
        defineAsset("misc/footprint.png", AssetType.Binary);
        defineAsset("misc/grasscolor.png", AssetType.Binary);
        defineAsset("misc/mapbg.png", AssetType.Binary);
        defineAsset("misc/mapicons.png", AssetType.Binary);
        defineAsset("misc/pumpkinblur.png", AssetType.Binary);
        defineAsset("misc/shadow.png", AssetType.Binary);
        defineAsset("misc/vignette.png", AssetType.Binary);
        defineAsset("misc/water.png", AssetType.Binary);
        defineAsset("misc/watercolor.png", AssetType.Binary);

        defineAsset("mob/char.png", AssetType.Binary);
        defineAsset("mob/chicken.png", AssetType.Binary);
        defineAsset("mob/cow.png", AssetType.Binary);
        defineAsset("mob/creeper.png", AssetType.Binary);
        defineAsset("mob/ghast.png", AssetType.Binary);
        defineAsset("mob/ghast_fire.png", AssetType.Binary);
        defineAsset("mob/pig.png", AssetType.Binary);
        defineAsset("mob/pigman.png", AssetType.Binary);
        defineAsset("mob/pigzombie.png", AssetType.Binary);
        defineAsset("mob/saddle.png", AssetType.Binary);
        defineAsset("mob/sheep.png", AssetType.Binary);
        defineAsset("mob/sheep_fur.png", AssetType.Binary);
        defineAsset("mob/silverfish.png", AssetType.Binary);
        defineAsset("mob/skeleton.png", AssetType.Binary);
        defineAsset("mob/slime.png", AssetType.Binary);
        defineAsset("mob/spider.png", AssetType.Binary);
        defineAsset("mob/spider_eyes.png", AssetType.Binary);
        defineAsset("mob/squid.png", AssetType.Binary);
        defineAsset("mob/wolf.png", AssetType.Binary);
        defineAsset("mob/wolf_angry.png", AssetType.Binary);
        defineAsset("mob/wolf_tame.png", AssetType.Binary);
        defineAsset("mob/zombie.png", AssetType.Binary);

        defineAsset("terrain/moon.png", AssetType.Binary);
        defineAsset("terrain/sun.png", AssetType.Binary);

        defineAsset("pack.png", AssetType.Binary);
        defineAsset("pack.txt", AssetType.Text);

        defineAsset("particles.png", AssetType.Binary);

        defineAsset("terrain.png", AssetType.Binary);

        extractNeccessaryAssets();
        loadAssets();

        defineEmbeddedAsset("shaders/chunk.vert", AssetType.Text);
        defineEmbeddedAsset("shaders/chunk.frag", AssetType.Text);

        _logger.LogInformation($"Loaded {embeddedAssetsLoaded} embedded assets");
    }

    public Asset getAsset(string assetPath)
    {
        if (assetPath.StartsWith('/'))
        {
            assetPath = assetPath[1..];
        }

        if (loadedAssets.TryGetValue(assetPath, out Asset? asset))
        {
            return asset;
        }
        else
        {
            throw new Exception($"Unknown asset: {assetPath}");
        }
    }

    private void extractNeccessaryAssets()
    {
        Directory.CreateDirectory("assets");

        foreach (var directory in assetDirectories)
        {
            Directory.CreateDirectory("assets/" + directory);
        }

        assetDirectories.Clear();

        using ZipArchive archive = ZipFile.OpenRead("b1.7.3.jar");
        Dictionary<string, ZipArchiveEntry> entries = [];

        foreach (var entry in archive.Entries)
        {
            entries[entry.FullName] = entry;
        }

        foreach (var assetPath in assetsToLoad.Keys)
        {
            var fsAssetPath = "assets/" + assetPath;

            if (!File.Exists(fsAssetPath))
            {
                if (entries.TryGetValue(assetPath, out ZipArchiveEntry? entry))
                {
                    entry.ExtractToFile(fsAssetPath);
                }
                else
                {
                    throw new Exception($"Asset does not exist in jar: {assetPath}");
                }
            }
        }
    }

    private void loadAssets()
    {
        foreach (var kvp in assetsToLoad)
        {
            string assetPath = kvp.Key;
            AssetType type = kvp.Value;

            if (type == AssetType.Binary)
            {
                try
                {
                    loadedAssets[assetPath] = new(File.ReadAllBytes("assets/" + assetPath));
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to load binary asset: {assetPath}, {e}");
                }
            }
            else if (type == AssetType.Text)
            {
                try
                {
                    loadedAssets[assetPath] = new(File.ReadAllText("assets/" + assetPath));
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to load text asset: {assetPath}, {e}");
                }
            }
        }

        _logger.LogInformation($"Loaded {assetsToLoad.Count} assets");

        assetsToLoad.Clear();
    }

    private void defineAsset(string assetPath, AssetType type)
    {
        assetsToLoad[assetPath] = type;

        int idx = assetPath.IndexOf('/');
        if (idx != -1)
        {
            string directory = assetPath[..idx];
            assetDirectories.Add(directory);
        }
    }

    private void defineEmbeddedAsset(string embeddedAssetPath, AssetType type)
    {
        var embeddedAssetPathForPath = embeddedAssetPath.Replace('/', '.');

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"{nameof(BetaSharp)}." + embeddedAssetPathForPath;

            using Stream? stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Embedded resource not found: " + resourceName);
            switch (type)
            {
                case AssetType.Text:
                    {
                        using var reader = new StreamReader(stream);
                        string text = reader.ReadToEnd();
                        loadedAssets[embeddedAssetPath] = new(text);
                        embeddedAssetsLoaded++;
                        break;
                    }

                case AssetType.Binary:
                    {
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        loadedAssets[embeddedAssetPath] = new(ms.ToArray());
                        embeddedAssetsLoaded++;
                        break;
                    }
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Exception while loading embedded asset: {e}");
        }
    }
}
