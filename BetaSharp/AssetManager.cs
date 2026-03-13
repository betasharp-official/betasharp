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

    public enum AssetProfile
    {
        Full,
        Headless
    }

    public class Asset
    {
        private readonly AssetType _type;
        private readonly byte[]? _binaryContent;
        private readonly string? _textContent;

        public Asset(byte[] binary)
        {
            _type = AssetType.Binary;
            _binaryContent = binary;
        }

        public Asset(string text)
        {
            _type = AssetType.Text;
            _textContent = text;
        }

        public AssetType GetAssetType() => _type;

        public byte[] GetBinaryContent()
        {
            if (_binaryContent == null || _type != AssetType.Binary)
            {
                throw new Exception("Attempted to get binary content from a non binary asset");
            }

            return _binaryContent;
        }

        public string GetTextContent()
        {
            if (_textContent == null || _type != AssetType.Text)
            {
                throw new Exception("Attempted to get text content from a non text asset");
            }

            return _textContent;
        }
    }

    private static readonly object s_instanceLock = new();
    private static AssetManager? s_instance;
    private static AssetProfile? s_configuredProfile;

    public static AssetManager Instance
    {
        get
        {
            lock (s_instanceLock)
            {
                s_instance ??= new AssetManager(s_configuredProfile ?? AssetProfile.Full);
                return s_instance;
            }
        }
    }

    public static void Initialize(AssetProfile profile)
    {
        lock (s_instanceLock)
        {
            if (s_instance != null)
            {
                if (s_instance._assetProfile != profile)
                {
                    throw new InvalidOperationException($"AssetManager already initialized with profile {s_instance._assetProfile}, cannot reinitialize with {profile}.");
                }

                return;
            }

            s_configuredProfile = profile;
            s_instance = new AssetManager(profile);
        }
    }

    private readonly Dictionary<string, AssetType> _assetsToLoad = [];
    private readonly Dictionary<string, Asset> _loadedAssets = [];
    private readonly HashSet<string> _assetDirectories = [];
    private int _embeddedAssetsLoaded;
    private readonly AssetProfile _assetProfile;
    private readonly ILogger<AssetManager> _logger = Log.Instance.For<AssetManager>();

    private AssetManager(AssetProfile assetProfile)
    {
        _assetProfile = assetProfile;

        defineHeadlessAssets();

        if (_assetProfile == AssetProfile.Full)
        {
            defineFullAssets();
        }

        _logger.LogInformation($"Asset profile: {_assetProfile}. Registered {_assetsToLoad.Count} assets.");

        extractNeccessaryAssets();
        loadAssets();

        _logger.LogInformation($"Loaded {_embeddedAssetsLoaded} embedded assets");
    }

    private void defineHeadlessAssets()
    {
        defineAsset("font.txt", AssetType.Text);
        defineAsset("achievement/map.txt", AssetType.Text);
        defineAsset("lang/en_US.lang", AssetType.Text);
        defineAsset("lang/stats_US.lang", AssetType.Text);
    }

    private void defineFullAssets()
    {
        defineAsset("title/splashes.txt", AssetType.Text);
        defineAsset("title/black.png", AssetType.Binary);
        defineAsset("title/mclogo.png", AssetType.Binary);
        defineAsset("title/mojang.png", AssetType.Binary);
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
        defineAsset("gui/Pointer.png", AssetType.Binary);

        string[] controllerPlatforms = ["ps3", "ps4", "ps5", "xone"];
        string[] controllerIcons = [
            "back_button", "back_button_pressed", "down_button", "down_button_pressed",
            "dpad_down", "dpad_down_pressed", "dpad_left", "dpad_left_pressed",
            "dpad_right", "dpad_right_pressed", "dpad_up", "dpad_up_pressed",
            "guide_button", "left_bumper", "left_bumper_pressed", "left_button",
            "left_button_pressed", "left_stick", "left_stick_button",
            "left_stick_button_pressed", "left_stick_pressed_left",
            "left_stick_pressed_right", "left_trigger", "left_trigger_pressed",
            "right_bumper", "right_bumper_pressed", "right_button",
            "right_button_pressed", "right_stick", "right_stick_button",
            "right_stick_button_pressed", "right_stick_pressed_left",
            "right_stick_pressed_right", "right_trigger", "right_trigger_pressed",
            "start_button", "start_button_pressed", "unknown", "up_button",
            "up_button_pressed"
        ];

        foreach (string platform in controllerPlatforms)
        {
            foreach (string icon in controllerIcons)
            {
                defineAsset($"gui/controls/{platform}/{icon}.png", AssetType.Binary);
            }

            if (platform == "ps4" || platform == "ps5")
            {
                defineAsset($"gui/controls/{platform}/touchpad.png", AssetType.Binary);
                defineAsset($"gui/controls/{platform}/touchpad_pressed.png", AssetType.Binary);
            }
        }

        defineAsset("gui/world_types/default.png", AssetType.Binary);
        defineAsset("gui/world_types/flat.png", AssetType.Binary);
        defineAsset("gui/world_types/sky.png", AssetType.Binary);

        defineAsset("item/arrows.png", AssetType.Binary);
        defineAsset("item/boat.png", AssetType.Binary);
        defineAsset("item/cart.png", AssetType.Binary);
        defineAsset("item/door.png", AssetType.Binary);
        defineAsset("item/sign.png", AssetType.Binary);

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

        defineEmbeddedAsset("shaders/chunk.vert", AssetType.Text);
        defineEmbeddedAsset("shaders/chunk.frag", AssetType.Text);
    }

    public Asset getAsset(string assetPath)
    {
        if (assetPath.StartsWith('/'))
        {
            assetPath = assetPath[1..];
        }

        if (_loadedAssets.TryGetValue(assetPath, out Asset? asset))
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

        using ZipArchive archive = ZipFile.OpenRead("b1.7.3.jar");
        Dictionary<string, ZipArchiveEntry> entries = [];
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            entries[entry.FullName] = entry;
        }

        foreach (string assetPath in _assetsToLoad.Keys)
        {
            string fsAssetPath = Path.Combine("assets", assetPath);
            string? directory = Path.GetDirectoryName(fsAssetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(fsAssetPath))
            {
                if (entries.TryGetValue(assetPath, out ZipArchiveEntry? entry))
                {
                    entry.ExtractToFile(fsAssetPath);
                }
                else
                {
                    _logger.LogWarning($"Asset does not exist in jar: {assetPath}. Ensuring it exists locally.");
                    if (!File.Exists(fsAssetPath))
                    {
                        _logger.LogError($"Asset {assetPath} is missing both from jar and local assets folder!");
                    }
                }
            }
        }
    }

    private void loadAssets()
    {
        foreach (KeyValuePair<string, AssetType> kvp in _assetsToLoad)
        {
            string assetPath = kvp.Key;
            AssetType type = kvp.Value;

            if (type == AssetType.Binary)
            {
                try
                {
                    _loadedAssets[assetPath] = new(File.ReadAllBytes("assets/" + assetPath));
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
                    _loadedAssets[assetPath] = new(File.ReadAllText("assets/" + assetPath));
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to load text asset: {assetPath}, {e}");
                }
            }
        }

        _logger.LogInformation($"Loaded {_assetsToLoad.Count} assets");

        _assetsToLoad.Clear();
    }

    private void defineAsset(string assetPath, AssetType type)
    {
        _assetsToLoad[assetPath] = type;

        int idx = assetPath.IndexOf('/');
        if (idx != -1)
        {
            string directory = assetPath[..idx];
            _assetDirectories.Add(directory);
        }
    }

    private void defineEmbeddedAsset(string embeddedAssetPath, AssetType type)
    {
        string embeddedAssetPathForPath = embeddedAssetPath.Replace('/', '.');

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
                        _loadedAssets[embeddedAssetPath] = new(text);
                        _embeddedAssetsLoaded++;
                        break;
                    }

                case AssetType.Binary:
                    {
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        _loadedAssets[embeddedAssetPath] = new(ms.ToArray());
                        _embeddedAssetsLoaded++;
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
