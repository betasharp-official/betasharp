using System;
using System.IO;
using BetaSharp.Client.Input;
using Microsoft.Extensions.Logging;
using File = System.IO.File;
using FileNotFoundException = System.IO.FileNotFoundException;

namespace BetaSharp.Client.Options;

public class GameOptions
{
    private readonly ILogger<GameOptions> _logger = Log.Instance.For<GameOptions>();

    private static readonly string[] DifficultyLabels =
    [
        "options.difficulty.peaceful",
        "options.difficulty.easy",
        "options.difficulty.normal",
        "options.difficulty.hard",
    ];
    private static readonly string[] GuiScaleLabels =
    [
        "options.guiScale.auto",
        "options.guiScale.small",
        "options.guiScale.normal",
        "options.guiScale.large",
    ];

    private static readonly string[] AnisoLabels = ["options.off", "2x", "4x", "8x", "16x"];
    private static readonly string[] MSAALabels = ["options.off", "2x", "4x", "8x"];

    public static float MaxAnisotropy = 1.0f;


    public FloatOption MusicVolumeOption { get; private set; }
    public FloatOption SoundVolumeOption { get; private set; }
    public FloatOption MouseSensitivityOption { get; private set; }
    public FloatOption FramerateLimitOption { get; private set; }
    public FloatOption FovOption { get; private set; }


    public BoolOption InvertMouseOption { get; private set; }
    public BoolOption ViewBobbingOption { get; private set; }
    public BoolOption VSyncOption { get; private set; }
    public BoolOption MipmapsOption { get; private set; }
    public BoolOption DebugModeOption { get; private set; }
    public BoolOption RenderOccludedOption { get; private set; }
    public BoolOption EnvironmentAnimationOption { get; private set; }
    public BoolOption ChunkFadeOption { get; private set; }
    public BoolOption MenuMusicOption { get; private set; }


    public FloatOption RenderDistanceOption { get; private set; }
    public CycleOption DifficultyOption { get; private set; }
    public CycleOption GuiScaleOption { get; private set; }
    public CycleOption AnisotropicOption { get; private set; }
    public CycleOption MsaaOption { get; private set; }


    public GameOption[] MainScreenOptions => [DifficultyOption, FovOption];
    public GameOption[] AudioScreenOptions => [MusicVolumeOption, SoundVolumeOption, MenuMusicOption];
    public GameOption[] VideoScreenOptions =>
    [
        RenderDistanceOption, FramerateLimitOption, VSyncOption,
        ViewBobbingOption, GuiScaleOption, AnisotropicOption,
        MipmapsOption, MsaaOption, EnvironmentAnimationOption, ChunkFadeOption
    ];

    public GameOption[] DebugScreenOptions => [DebugModeOption, RenderOccludedOption];


    public float MusicVolume
    {
        get => MusicVolumeOption.Value;
        set => MusicVolumeOption.Value = value;
    }

    public float SoundVolume
    {
        get => SoundVolumeOption.Value;
        set => SoundVolumeOption.Value = value;
    }

    public float MouseSensitivity => MouseSensitivityOption.Value;
    public float LimitFramerate => FramerateLimitOption.Value;
    public float Fov => FovOption.Value;
    public bool InvertMouse
    {
        get => InvertMouseOption.Value;
        set => InvertMouseOption.Value = value;
    }
    public int renderDistance => 4 + (int)(RenderDistanceOption.Value * 28.0f);
    public bool ViewBobbing => ViewBobbingOption.Value;
    public bool VSync => VSyncOption.Value;
    public int Difficulty => DifficultyOption.Value;
    public int GuiScale => GuiScaleOption.Value;
    public int AnisotropicLevel => AnisotropicOption.Value;
    public int MSAALevel => MsaaOption.Value;
    public int INITIAL_MSAA;
    public bool UseMipmaps => MipmapsOption.Value;
    public bool DebugMode => DebugModeOption.Value;
    public bool RenderOccluded => RenderOccludedOption.Value;
    public bool EnvironmentAnimation => EnvironmentAnimationOption.Value;
    public bool ChunkFade => ChunkFadeOption.Value;
    public bool MenuMusic => MenuMusicOption.Value;


    public string Skin = "Default";
    public KeyBinding KeyBindForward = new("key.forward", 17);
    public KeyBinding KeyBindLeft = new("key.left", 30);
    public KeyBinding KeyBindBack = new("key.back", 31);
    public KeyBinding KeyBindRight = new("key.right", 32);
    public KeyBinding KeyBindJump = new("key.jump", 57);
    public KeyBinding KeyBindInventory = new("key.inventory", 18);
    public KeyBinding KeyBindDrop = new("key.drop", 16);
    public KeyBinding KeyBindChat = new("key.chat", 20);
    public KeyBinding KeyBindCommand = new("key.command", Keyboard.KEY_SLASH);
    public KeyBinding KeyBindToggleFog = new("key.fog", 33);
    public KeyBinding KeyBindSneak = new("key.sneak", 42);
    public KeyBinding[] KeyBindings;

    protected BetaSharp _game;
    private readonly string _optionsPath;
    public bool HideGUI = false;
    public EnumCameraMode CameraMode = EnumCameraMode.FirstPerson;
    public bool ShowDebugInfo = false;
    public string LastServer = "";
    public bool InvertScrolling = false;
    public bool SmoothCamera = false;
    public bool DebugCamera = false;
    public float AmountScrolled = 1.0F;
    public float field_22271_G = 1.0F;
    private bool initialDebugMode;
    public float Brightness = 0.5F;


    private Dictionary<string, GameOption> _allOptions;

    public GameOptions(BetaSharp game, string gameDataDir)
    {
        _game = game;
        _optionsPath = System.IO.Path.Combine(gameDataDir, "options.txt");

        InitializeOptions();

        KeyBindings =
        [
            KeyBindForward,
            KeyBindLeft,
            KeyBindBack,
            KeyBindRight,
            KeyBindJump,
            KeyBindSneak,
            KeyBindDrop,
            KeyBindInventory,
            KeyBindChat,
            KeyBindToggleFog,
        ];

        LoadOptions();
        INITIAL_MSAA = MSAALevel;
        initialDebugMode = DebugMode;
    }

    public GameOptions()
    {
        InitializeOptions();
    }

    private void InitializeOptions()
    {
        MusicVolumeOption = new FloatOption("options.music", "music", 1.0F)
        {
            Steps = 100,
            OnChanged = _ => _game?.sndManager.OnSoundOptionsChanged()
        };
        SoundVolumeOption = new FloatOption("options.sound", "sound", 1.0F)
        {
            Steps = 100,
            OnChanged = _ => _game?.sndManager.OnSoundOptionsChanged()
        };
        MouseSensitivityOption = new FloatOption("options.sensitivity", "mouseSensitivity", 0.5F)
        {
            Steps = 200,
            Formatter = (v, t) => v == 0.0F
                ? t.TranslateKey("options.sensitivity.min")
                : v == 1.0F
                    ? t.TranslateKey("options.sensitivity.max")
                    : (int)(v * 200.0F) + "%"
        };
        FramerateLimitOption = new FloatOption("options.framerateLimit", "fpsLimit", 0.42857143f)
        {
            LabelOverride = "Max FPS",
            Steps = 210,
            Formatter = (v, _) =>
            {
                int fps = 30 + (int)(v * 210.0f);
                return fps == 240 ? "Unlimited" : fps + " FPS";
            }
        };
        FovOption = new FloatOption("options.fov", "fov", 0.44444445F)
        {
            LabelOverride = "FOV",
            Steps = 90,
            Formatter = (v, _) => (30 + (int)(v * 90.0f)).ToString()
        };

        InvertMouseOption = new BoolOption("options.invertMouse", "invertYMouse");
        ViewBobbingOption = new BoolOption("options.viewBobbing", "bobView", true);
        VSyncOption = new BoolOption("VSync", "vsync")
        {
            LabelOverride = "VSync",
            OnChanged = v => Display.getGlfw().SwapInterval(v ? 1 : 0)
        };
        MipmapsOption = new BoolOption("Mipmaps", "useMipmaps", true)
        {
            OnChanged = _ =>
            {
                if (BetaSharp.INSTANCE?.textureManager != null)
                    BetaSharp.INSTANCE.textureManager.Reload();
            }
        };
        DebugModeOption = new BoolOption("Debug Mode", "debugMode")
        {
            Formatter = (v, t) =>
            {
                string result = v ? t.TranslateKey("options.on") : t.TranslateKey("options.off");
                if (v != initialDebugMode) result += " [!]";
                return result;
            },
            OnChanged = v => Profiling.Profiler.Enabled = v
        };
        RenderOccludedOption = new BoolOption("Render Occluded", "renderOccluded");
        EnvironmentAnimationOption = new BoolOption("Environment Anim", "envAnimation", true);
        ChunkFadeOption = new BoolOption("Chunk Fade", "chunkFade", true);
        MenuMusicOption = new BoolOption("Menu Music", "menuMusic", true);

        RenderDistanceOption = new FloatOption("options.renderDistance", "viewDistance", 0.2f)
        {
            LabelOverride = "Render Distance",
            Steps = 28,
            Formatter = (v, t) => $"{4 + (int)(v * 28.0f)} Chunks",
            OnChanged = _ => {
                if (_game?.internalServer != null)
                {
                    _game.internalServer.SetViewDistance(this.renderDistance);
                }
            }
        };
        DifficultyOption = new CycleOption("options.difficulty", "difficulty", DifficultyLabels, 2);
        GuiScaleOption = new CycleOption("options.guiScale", "guiScale", GuiScaleLabels);
        AnisotropicOption = new CycleOption("Aniso Level", "anisotropicLevel", AnisoLabels)
        {
            Formatter = (v, t) => v == 0 ? t.TranslateKey("options.off") : AnisoLabels[v],
            OnChanged = v =>
            {
                int anisoValue = v == 0 ? 0 : (int)System.Math.Pow(2, v);
                if (anisoValue > MaxAnisotropy)
                {
                    AnisotropicOption.Value = 0;
                }
                if (BetaSharp.INSTANCE?.textureManager != null)
                    BetaSharp.INSTANCE.textureManager.Reload();
            }
        };
        MsaaOption = new CycleOption("MSAA", "msaaLevel", MSAALabels)
        {
            Formatter = (v, t) =>
            {
                string result = v == 0 ? t.TranslateKey("options.off") : MSAALabels[v];
                if (v != INITIAL_MSAA) result += " (Reload required)";
                return result;
            }
        };

        _allOptions = new Dictionary<string, GameOption>();
        foreach (var option in GetAllOptions())
        {
            _allOptions[option.SaveKey] = option;
        }
    }

    private IEnumerable<GameOption> GetAllOptions()
    {
        yield return MusicVolumeOption;
        yield return SoundVolumeOption;
        yield return MouseSensitivityOption;
        yield return FramerateLimitOption;
        yield return FovOption;
        yield return InvertMouseOption;
        yield return ViewBobbingOption;
        yield return VSyncOption;
        yield return MipmapsOption;
        yield return DebugModeOption;
        yield return RenderOccludedOption;
        yield return EnvironmentAnimationOption;
        yield return ChunkFadeOption;
        yield return MenuMusicOption;
        yield return RenderDistanceOption;
        yield return DifficultyOption;
        yield return GuiScaleOption;
        yield return AnisotropicOption;
        yield return MsaaOption;
    }


    public string GetKeyBindingDescription(int keyBindingIndex)
    {
        TranslationStorage translations = TranslationStorage.Instance;
        return translations.TranslateKey(KeyBindings[keyBindingIndex].keyDescription);
    }

    public string GetOptionDisplayString(int keyBindingIndex)
    {
        return Keyboard.getKeyName(KeyBindings[keyBindingIndex].keyCode);
    }

    public void SetKeyBinding(int keyBindingIndex, int keyCode)
    {
        KeyBindings[keyBindingIndex].keyCode = keyCode;
        SaveOptions();
    }


    public void LoadOptions()
    {
        try
        {
            if (!File.Exists(_optionsPath)) throw new FileNotFoundException($"Options file not found at {_optionsPath}");
            using StreamReader reader = new StreamReader(_optionsPath);
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                try
                {
                    string[] parts = line.Split(':');
                    if (parts.Length >= 2) LoadOptionFromParts(parts);
                }
                catch (Exception)
                {
                    _logger.LogError($"Skipping bad option: {line}");
                }
            }
        }
        catch (Exception)
        {
            _logger.LogError("Failed to load options");
        }
    }

    private void LoadOptionFromParts(string[] parts)
    {
        if (parts.Length < 2) return;

        string key = parts[0];
        string value = parts[1];

        if (_allOptions.TryGetValue(key, out GameOption? option))
        {
            option.Load(value);
            return;
        }

        switch (key)
        {
            case "skin": Skin = value; break;
            case "lastServer": LastServer = value; break;
            case "cameraMode": CameraMode = (EnumCameraMode)int.Parse(value); break;
            case "thirdPersonView":
                CameraMode = value == "true" ? EnumCameraMode.ThirdPerson : EnumCameraMode.FirstPerson;
                break;
            default:
                if (key.StartsWith("key_"))
                {
                    string bindName = key[4..];
                    for (int i = 0; i < KeyBindings.Length; ++i)
                    {
                        if (KeyBindings[i].keyDescription == bindName)
                        {
                            KeyBindings[i].keyCode = int.Parse(value);
                            break;
                        }
                    }
                }
                break;
        }
    }

    public void SaveOptions()
    {
        try
        {
            using var writer = new StreamWriter(_optionsPath);

            foreach (var option in GetAllOptions())
            {
                writer.WriteLine($"{option.SaveKey}:{option.Save()}");
            }

            writer.WriteLine($"skin:{Skin}");
            writer.WriteLine($"lastServer:{LastServer}");
            writer.WriteLine($"cameraMode:{(int)CameraMode}");

            foreach (var bind in KeyBindings)
            {
                writer.WriteLine($"key_{bind.keyDescription}:{bind.keyCode}");
            }

            writer.Close();
        }
        catch (Exception exception)
        {
            _logger.LogError($"Failed to save options: {exception.Message}");
        }
    }

    public void OnSoundOptionsChanged()
    {
        _game?.sndManager.OnSoundOptionsChanged();
    }
}
