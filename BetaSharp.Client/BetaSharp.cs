using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using BetaSharp.Blocks;
using BetaSharp.Client.Achievements;
using BetaSharp.Client.Debug;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.DynamicTexture;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Input;
using BetaSharp.Client.Network;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.Rendering.PostProcessing;
using BetaSharp.Client.Resource;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.Sound;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Screens;
using BetaSharp.Client.UI.Screens.InGame;
using BetaSharp.Client.UI.Screens.InGame.Containers;
using BetaSharp.Client.UI.Screens.Menu;
using BetaSharp.Client.UI.Screens.Menu.Net;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Profiling;
using BetaSharp.Server.Internal;
using BetaSharp.Stats;
using BetaSharp.Util;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.ClientData.Colors;
using BetaSharp.Worlds.Colors;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client;

public partial class BetaSharp :
    IScreenNavigator,
    IControllerState,
    IClientPlayerHost,
    IWorldHost,
    IInternalServerHost,
    ISingleplayerHost
{
    #region Constants & Static Members

    public static string Version { get; private set; } = UnknownVersion;
    public static string BetaSharpDir => PathHelper.GetAppDir(nameof(BetaSharp));
    public static long HasPaidCheckTime { get; private set; }

    private const string UnknownVersion = "unknown version";
    private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    #endregion

    #region Core Game State

    public volatile bool Running = true;
    public volatile bool IsGamePaused;

    public Timer Timer { get; } = new(20.0F);
    public int TicksRan { get; private set; }
    public Session Session { get; private set; }
    public GameOptions Options { get; private set; }
    public IWorldStorageSource SaveLoader { get; private set; }
    public InternalServer? InternalServer { get; private set; }

    #endregion

    #region World & Player Data

    public World World { get; private set; }
    World? IWorldHost.World => World;
    void IWorldHost.ChangeWorld(World? world) => ChangeWorld(world);

    public ClientPlayerEntity Player { get; private set; }
    public EntityLiving Camera => Player;
    ClientPlayerEntity? IClientPlayerHost.Player => Player;

    public PlayerController PlayerController { get; set; }
    void IClientPlayerHost.SetPlayerController(PlayerController controller) => PlayerController = controller;

    #endregion

    #region Rendering & Display Systems

    public int DisplayWidth { get; private set; }
    public int DisplayHeight { get; private set; }
    public bool ShowChunkBorders { get; private set; }
    public bool SkipRenderWorld { get; private set; }
    public string DebugText { get; private set; } = "";
    public HitResult ObjectMouseOver = new(HitResultType.MISS);

    public GameRenderer GameRenderer { get; private set; }
    public WorldRenderer WorldRenderer { get; private set; }
    public PostProcessManager PostProcessManager { get; private set; }
    public TextureManager TextureManager { get; private set; }
    public SkinManager SkinManager { get; private set; }
    public TextRenderer TextRenderer { get; private set; }
    public TexturePacks TexturePackList { get; private set; }
    public ParticleManager ParticleManager { get; private set; }

    #endregion

    #region UI & Input Systems

    public UIContext UIContext { get; private set; } = null!;
    public UIScreen? CurrentScreen { get; private set; }
    public HUD HUD { get; private set; } = null!;

    public MouseHelper MouseHelper { get; private set; }
    public VirtualCursor VirtualCursor { get; } = new();
    public bool IsControllerMode { get; set; }
    public int MouseTicksRan { get; set; }
    public bool InGameHasFocus { get; private set; }

    #endregion

    #region Audio & Diagnostics

    public SoundManager SoundManager { get; private set; } = new();
    public StatFileWriter StatFileWriter { get; private set; }
    public DebugComponentsStorage DebugComponentsStorage { get; private set; }

    #endregion

    #region Private Fields

    private readonly ILogger<BetaSharp> _logger = Log.Instance.For<BetaSharp>();
    private readonly LoadingScreenRenderer _loadingScreen;
    private readonly WaterSprite _textureWaterFX = new();
    private readonly LavaSprite _textureLavaFX = new();
    private readonly DebugTelemetry _debugTelemetry = new();

    private readonly string _serverName;
    private readonly int _serverPort;
    private readonly bool _hideQuitButton;

    private ImGuiController _imGuiController;
    private GLErrorHandler _glErrorHandler;
    private string _gameDataDir;

    private bool _fullscreen;
    private bool _prevF11Down;
    private bool _hasCrashed;
    private bool _isTakingScreenshot;

    private int _leftClickCounter;
    private int _tempDisplayWidth;
    private int _tempDisplayHeight;
    private int _joinPlayerCounter;

    private long _prevFrameTime = -1L;
    private long _systemTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private bool _isMainMenuOpen => CurrentScreen is MainMenuScreen;
    private bool _isGameOverOpen => CurrentScreen is GameOverScreen;

    #endregion

    #region Initialization & Lifecycle

    public BetaSharp(int width, int height, bool isFullscreen)
    {
        _loadingScreen = new LoadingScreenRenderer(this);
        _tempDisplayHeight = height;
        _fullscreen = isFullscreen;
        DisplayWidth = width;
        DisplayHeight = height;
    }

    public void StartGame()
    {
        LoadVersion();

        Bootstrap.Initialize();
        DebugComponents.RegisterComponents();

        InitializeTimer();

        SetupDisplay();
        SetupCoreSystems();

        LoadScreen();

        SetupOpenGLAndInput();
        SetupResourcesAndPostProcessing();

        CheckGLError("Post startup");

        StatFileWriter.ReadStat(Stats.Stats.StartGameStat, 1);
        if (_serverName != null)
        {
            Navigate(new ConnectingScreen(UIContext, CreateNetworkContext(), _serverName, _serverPort));
        }
        else
        {
            Navigate(CreateMainMenuScreen());
        }
    }

    private unsafe void SetupDisplay()
    {
        int maximumWidth = Display.getDisplayMode().getWidth();
        int maximumHeight = Display.getDisplayMode().getHeight();

        if (_fullscreen)
        {
            Display.setFullscreen(true);
            DisplayWidth = maximumWidth;
            DisplayHeight = maximumHeight;

            if (DisplayWidth <= 0) DisplayWidth = 1;
            if (DisplayHeight <= 0) DisplayHeight = 1;
        }
        else
        {
            Display.setDisplayMode(new DisplayMode(DisplayWidth, DisplayHeight));
            Display.setLocation((maximumWidth - DisplayWidth) / 2, (maximumHeight - DisplayHeight) / 2);
        }

        Display.setTitle("BetaSharp " + Version);

        _gameDataDir = BetaSharpDir;
        SaveLoader = new RegionWorldStorageSource(Path.Combine(_gameDataDir, "saves"));
        Options = new GameOptions(this, _gameDataDir);
        Options.ReloadTextures += () => { TextureManager.Reload(); };
        Options.ReloadChunks += () => { WorldRenderer.ChunkRenderer.MarkAllVisibleChunksDirty(); };

        DebugComponentsStorage = new DebugComponentsStorage(this, _gameDataDir);
        Profiler.Enabled = Options.DebugMode;
        Profiler.EnableLagSpikeDetection = Options.DebugMode;
        Profiler.LagSpikeDirectory = Path.Combine(_gameDataDir, "logs", "lag_spikes");

        try
        {
            int[] msaaValues = [0, 2, 4, 8];
            Display.MSAA_Samples = msaaValues[Options.MSAALevel];
            Display.DebugMode = Options.DebugMode;

            Display.create();
            Display.getGlfw().SetWindowSizeLimits(Display.getWindowHandle(), 850, 480, maximumWidth, maximumHeight);

            GLManager.Init(Display.getGL()!);
            if (GLManager.GL is LegacyGL legacyGl)
            {
                _debugTelemetry.CaptureSystemInfo(legacyGl);
            }
            else
            {
                _debugTelemetry.CaptureSystemInfo(null);
            }

            Display.getGlfw().SwapInterval(Options.VSync ? 1 : 0);

            if (Options.DebugMode)
            {
                _glErrorHandler = new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception initializing display");
        }
    }

    private void SetupCoreSystems()
    {
        TexturePackList = new TexturePacks(this, new DirectoryInfo(_gameDataDir));
        TextureManager = new TextureManager(this, TexturePackList, Options);
        TextRenderer = new TextRenderer(Options, TextureManager);

        UIContext = new UIContext(
            Options,
            TextRenderer,
            TextureManager,
            playClickSound: () => SoundManager.PlaySoundFX("random.click", 1.0f, 1.0f),
            displaySize: () => new Vector2D<int>(DisplayWidth, DisplayHeight),
            controllerState: this,
            VirtualCursor,
            Timer,
            navigator: this,
            hasWorld: () => World != null
        );

        SkinManager = new SkinManager(TextureManager);
        WaterColors.loadColors(TextureManager.GetColors("/misc/watercolor.png"));
        GrassColors.loadColors(TextureManager.GetColors("/misc/grasscolor.png"));
        FoliageColors.loadColors(TextureManager.GetColors("/misc/foliagecolor.png"));
        GameRenderer = new GameRenderer(this);
        EntityRenderDispatcher.Instance.SkinManager = SkinManager;
        EntityRenderDispatcher.Instance.HeldItemRenderer = new HeldItemRenderer(this);
        StatFileWriter = new StatFileWriter(Session, _gameDataDir);

        StatStringFormatKeyInv format = new(this);
        global::BetaSharp.Achievements.OpenInventory.GetTranslatedDescription = () =>
        {
            return format.formatString(global::BetaSharp.Achievements.OpenInventory.TranslationKey);
        };
    }

    private unsafe void SetupOpenGLAndInput()
    {
        bool anisotropicFiltering = GLManager.GL.IsExtensionPresent("GL_EXT_texture_filter_anisotropic");
        _logger.LogInformation($"Anisotropic Filtering Supported: {anisotropicFiltering}");

        if (anisotropicFiltering)
        {
            GLManager.GL.GetFloat(GLEnum.MaxTextureMaxAnisotropy, out float maxAnisotropy);
            GameOptions.MaxAnisotropy = maxAnisotropy;
            _logger.LogInformation($"Max Anisotropy: {maxAnisotropy}");
        }
        else
        {
            GameOptions.MaxAnisotropy = 1.0f;
        }

        try
        {
            IWindow window = Display.getWindow();
            IInputContext input = window.CreateInput();
            _imGuiController = new(((LegacyGL)GLManager.GL).SilkGL, window, input);
            _imGuiController.MakeCurrent();
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to initialize ImGui: {e}");
            _imGuiController = null;
        }

        Keyboard.create(Display.getGlfw(), Display.getWindowHandle());
        Mouse.create(Display.getGlfw(), Display.getWindowHandle(), Display.getWidth(), Display.getHeight());
        Controller.Create(Display.getGlfw(), Display.getWindowHandle());
        ControllerManager.Initialize(this);
        MouseHelper = new MouseHelper();

        CheckGLError("Pre startup");
        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.ShadeModel(GLEnum.Smooth);
        GLManager.GL.ClearDepth(1.0D);
        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.DepthFunc(GLEnum.Lequal);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.AlphaFunc(GLEnum.Greater, 0.1F);
        GLManager.GL.CullFace(GLEnum.Back);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        CheckGLError("Startup");
    }

    private void SetupResourcesAndPostProcessing()
    {
        SoundManager.LoadSoundSettings(Options);
        DefaultMusicCategories.Register(SoundManager);

        TextureManager.AddDynamicTexture(_textureLavaFX);
        TextureManager.AddDynamicTexture(_textureWaterFX);
        TextureManager.AddDynamicTexture(new NetherPortalSprite());
        TextureManager.AddDynamicTexture(new CompassSprite(this));
        TextureManager.AddDynamicTexture(new ClockSprite(this));
        TextureManager.AddDynamicTexture(new WaterSideSprite());
        TextureManager.AddDynamicTexture(new LavaSideSprite());
        TextureManager.AddDynamicTexture(new FireSprite(0));
        TextureManager.AddDynamicTexture(new FireSprite(1));

        WorldRenderer = new WorldRenderer(this, TextureManager);
        GLManager.GL.Viewport(0, 0, (uint)Display.getFramebufferWidth(), (uint)Display.getFramebufferHeight());
        ParticleManager = new ParticleManager(World, TextureManager);

        _ = new ResourceManager()
            .Add(new BetaResourceDownloader(this, _gameDataDir))
            .Add(new ModernAssetDownloader(this, _gameDataDir,
            [
                "minecraft/sounds/music/menu/moog_city_2.ogg",
                "minecraft/sounds/music/menu/mutation.ogg",
                "minecraft/sounds/music/menu/floating_trees.ogg",
                "minecraft/sounds/music/menu/beginning_2.ogg",
            ])).LoadAllAsync();

        HUD = new HUD(UIContext, new HUDContext(
            () => Player,
            () => PlayerController,
            () => World,
            DebugComponentsStorage,
            () => CurrentScreen == null && Player != null && World != null
                ? new InGameTipContext(ObjectMouseOver, World.Reader, Player.inventory.getSelectedItem())
                : null,
            () => _isMainMenuOpen
        ));

        PostProcessManager = new PostProcessManager(Display.getFramebufferWidth(), Display.getFramebufferHeight(), Options);
    }

    private void LoadVersion()
    {
        try
        {
            if (File.Exists("version.txt"))
            {
                Version = File.ReadAllText("version.txt").Trim().ToLower();
            }
            else
            {
                Version = "development build";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to load version: {}", ex.Message);
            Version = UnknownVersion;
        }
    }

    public void Shutdown()
    {
        Running = false;
    }

    private void ShutdownGame()
    {
        try
        {
            StopInternalServer();
            StatFileWriter.Tick();
            StatFileWriter.SyncStats();

            _logger.LogInformation("Stopping!");

            try { ChangeWorld(null); } catch (Exception) { }
            try { GLAllocation.deleteTexturesAndDisplayLists(); } catch (Exception) { }

            SkinManager.Dispose();
            TextureManager.Dispose();
            SoundManager.Dispose();
            Mouse.destroy();
            Keyboard.destroy();

            GLTexture.LogLeakReport();
        }
        finally
        {
            Display.destroy();
            CleanupTimer();

            if (!_hasCrashed)
            {
                Environment.Exit(0);
            }
        }
    }

    public void CrashCleanup()
    {
        try
        {
            ChangeWorld(null);
        }
        catch (Exception)
        {
        }
    }

    public void OnGameCrash(Exception crashInfo)
    {
        _hasCrashed = true;
        _logger.LogError(crashInfo, "BetaSharp has crashed!");
    }

    #endregion

    #region Main Game Loop

    public void Run()
    {
        Running = true;

        try
        {
            StartGame();
        }
        catch (Exception startupException)
        {
            OnGameCrash(startupException);
            return;
        }

        try
        {
            long lastFpsCheckTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int frameCounter = 0;

            while (Running)
            {
                long frameStartNano = Stopwatch.GetTimestamp();

                int startGcGen0 = GC.CollectionCount(0);
                int startGcGen1 = GC.CollectionCount(1);
                int startGcGen2 = GC.CollectionCount(2);

                if (Options.DebugMode)
                {
                    Profiler.Update(Timer.DeltaTime);
                    Profiler.PushGroup("run");
                }

                try
                {
                    if (Display.isCloseRequested())
                    {
                        Shutdown();
                    }

                    Controller.PollEvents();
                    if (Controller.IsActive() && !IsControllerMode)
                    {
                        Mouse.setCursorVisible(false);
                        IsControllerMode = true;
                    }

                    if (IsControllerMode && CurrentScreen != null)
                    {
                        VirtualCursor.Update(CurrentScreen, Options, DisplayWidth, DisplayHeight, Timer.DeltaTime);
                    }

                    if (IsGamePaused && World != null)
                    {
                        float previousRenderPartialTicks = Timer.renderPartialTicks;
                        Timer.UpdateTimer();
                        Timer.renderPartialTicks = previousRenderPartialTicks;
                    }
                    else
                    {
                        Timer.UpdateTimer();
                    }

                    long tickStartTime = Stopwatch.GetTimestamp();
                    if (Options.DebugMode) Profiler.PushGroup("runTicks");

                    for (int tickIndex = 0; tickIndex < Timer.elapsedTicks; ++tickIndex)
                    {
                        ++TicksRan;
                        RunTick(Timer.renderPartialTicks);
                    }

                    if (Options.DebugMode) Profiler.PopGroup();

                    long tickElapsedTime = Stopwatch.GetTimestamp() - tickStartTime;
                    CheckGLError("Pre render");

                    SoundManager.UpdateListener(Player, Timer.renderPartialTicks);
                    GLManager.GL.Enable(GLEnum.Texture2D);

                    if (World != null)
                    {
                        if (Options.DebugMode) Profiler.Start("updateLighting");
                        World.Lighting.DoLightingUpdates();
                        if (Options.DebugMode) Profiler.Stop("updateLighting");
                    }

                    if (!Keyboard.isKeyDown(Keyboard.KEY_F7))
                    {
                        if (Options.DebugMode) Profiler.Start("wait");
                        Display.update();
                        if (Options.DebugMode) Profiler.Stop("wait");
                    }

                    if (Player != null && Player.isInsideWall())
                    {
                        Options.CameraMode = EnumCameraMode.FirstPerson;
                    }

                    if (!SkipRenderWorld)
                    {
                        PlayerController?.setPartialTime(Timer.renderPartialTicks);

                        if (Options.DebugMode)
                        {
                            Profiler.PushGroup("render");
                            TextureStats.StartFrame();
                        }

                        GameRenderer.onFrameUpdate(Timer.renderPartialTicks);

                        if (Options.DebugMode)
                        {
                            TextureStats.EndFrame();
                            Profiler.PopGroup();
                        }
                    }

                    if (_imGuiController != null && Timer.DeltaTime > 0.0f && Options.ShowDebugInfo && Options.DebugMode)
                    {
                        RenderDebugUI();
                    }

                    if (!Display.isActive())
                    {
                        if (_fullscreen) ToggleFullscreen();
                        Thread.Sleep(10);
                    }

                    _prevFrameTime = Stopwatch.GetTimestamp();

                    if (Keyboard.isKeyDown(Keyboard.KEY_F7))
                    {
                        Display.update();
                    }

                    ScreenshotListener();

                    if (Display.wasResized())
                    {
                        DisplayWidth = Display.getWidth();
                        DisplayHeight = Display.getHeight();
                        if (DisplayWidth <= 0) DisplayWidth = 1;
                        if (DisplayHeight <= 0) DisplayHeight = 1;
                        Resize(DisplayWidth, DisplayHeight);
                    }

                    CheckGLError("Post render");
                    ++frameCounter;

                    IsGamePaused = (!IsMultiplayerWorld() || InternalServer != null) && (CurrentScreen?.PausesGame ?? false);

                    for (; DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= lastFpsCheckTime + 1000L; frameCounter = 0)
                    {
                        DebugText = frameCounter + " fps";
                        lastFpsCheckTime += 1000L;
                    }
                }
                catch (OutOfMemoryException)
                {
                    CrashCleanup();
                    Navigate(new ErrorScreen(UIContext, "Out of memory!", "Minecraft has run out of memory."));
                }
                finally
                {
                    ReportFrameTelemetry(frameStartNano, startGcGen0, startGcGen1, startGcGen2);
                }
            }
        }
        catch (BetaSharpShutdownException)
        {
        }
        catch (Exception unexpectedException)
        {
            CrashCleanup();
            OnGameCrash(unexpectedException);
        }
        finally
        {
            ShutdownGame();
        }
    }

    private void RenderDebugUI()
    {
        _imGuiController.Update(Timer.DeltaTime);
        ProfilerRenderer.Draw();
        ProfilerRenderer.DrawGraph();

        ImGui.Begin("Render Info");
        ImGui.Text($"Chunks Total: {WorldRenderer.ChunkRenderer.TotalChunks}");
        ImGui.Text($"Chunks Frustum: {WorldRenderer.ChunkRenderer.ChunksInFrustum}");
        ImGui.Text($"Chunks Occluded: {WorldRenderer.ChunkRenderer.ChunksOccluded}");
        ImGui.Text($"Chunks Rendered: {WorldRenderer.ChunkRenderer.ChunksRendered}");
        ImGui.Separator();
        ImGui.Text($"Chunk Vertex Buffer Allocated MB: {VertexBuffer<ChunkVertex>.Allocated / 1000000.0}");
        ImGui.Text($"ChunkMeshVersion Allocated: {ChunkMeshVersion.TotalAllocated}");
        ImGui.Text($"ChunkMeshVersion Released: {ChunkMeshVersion.TotalReleased}");

        WorldRenderer.ChunkRenderer.GetMeshSizeStats(out int minSize, out int maxSize, out int avgSize, out Dictionary<int, int> buckets);
        ImGui.Separator();
        int activeMeshes = 0;
        foreach (int v in buckets.Values) activeMeshes += v;
        ImGui.Text($"Active Meshes: {activeMeshes}");
        ImGui.Text($"Min Mesh Size: {minSize} bytes");
        ImGui.Text($"Max Mesh Size: {maxSize} bytes");
        ImGui.Text($"Avg Mesh Size: {avgSize} bytes");

        if (ImGui.TreeNode("Mesh Size Buckets (KB)"))
        {
            var sortedBuckets = buckets.Keys.ToList();
            sortedBuckets.Sort();
            foreach (int po2 in sortedBuckets)
            {
                ImGui.Text($"{po2}KB: {buckets[po2]} meshes");
            }
            ImGui.TreePop();
        }

        if (ImGui.Button("Export Mesh Stats to JSON"))
        {
            var exportData = new
            {
                minSize,
                maxSize,
                avgSize,
                totalMeshes = activeMeshes,
                buckets = buckets.ToDictionary(k => k.Key.ToString(), v => v.Value)
            };
            string json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(Path.Combine(BetaSharpDir, "mesh_stats.json"), json);
            _logger.LogInformation($"Exported mesh stats to {Path.Combine(BetaSharpDir, "mesh_stats.json")}");
        }

        ImGui.Separator();
        ImGui.Text($"Texture Binds: {TextureStats.BindsLastFrame} (Avg: {TextureStats.AverageBindsPerFrame:F1}/f)");
        ImGui.Text($"Active Textures: {GLTexture.ActiveTextureCount}");
        ImGui.End();

        _imGuiController.Render();
    }

    private void ReportFrameTelemetry(long frameStartNano, int startGcGen0, int startGcGen1, int startGcGen2)
    {
        long frameEndNano = Stopwatch.GetTimestamp();
        double thisFrameTimeMs = (frameEndNano - frameStartNano) / 1000000.0;
        _debugTelemetry.RecordFrameTime(thisFrameTimeMs);

        if (Options.DebugMode)
        {
            Profiler.Record("frame Time", thisFrameTimeMs);
            Profiler.CaptureFrame();
            Profiler.PopGroup();

            if (Display.isActive())
            {
                int endGcGen0 = GC.CollectionCount(0);
                int endGcGen1 = GC.CollectionCount(1);
                int endGcGen2 = GC.CollectionCount(2);

                int gc0Diff = endGcGen0 - startGcGen0;
                int gc1Diff = endGcGen1 - startGcGen1;
                int gc2Diff = endGcGen2 - startGcGen2;

                string gcContext = "";
                if (gc0Diff > 0 || gc1Diff > 0 || gc2Diff > 0)
                {
                    gcContext = $"GC Collections this frame: Gen0[{gc0Diff}] Gen1[{gc1Diff}] Gen2[{gc2Diff}]";
                }

                int fpsLimit = 30 + (int)(Options.LimitFramerate * 210.0f);
                double msPerFrameTarget = fpsLimit == 240 ? 16.666 : (1000.0 / fpsLimit);
                Profiler.LagSpikeThresholdMs = msPerFrameTarget * 2.0;
                Profiler.DetectLagSpike(thisFrameTimeMs, string.IsNullOrEmpty(gcContext) ? DebugText : $"{DebugText} - {gcContext}", true);
            }
        }
    }

    #endregion

    #region Tick Logic

    public void RunTick(float partialTicks)
    {
        Profiler.PushGroup("runTick");

        Profiler.Start("statFileWriter.SyncStatsIfReady");
        StatFileWriter.SyncStatsIfReady();
        Profiler.Stop("statFileWriter.SyncStatsIfReady");

        bool f11Down = Keyboard.isKeyDown(Keyboard.KEY_F11);
        if (f11Down && !_prevF11Down)
        {
            ToggleFullscreen();
        }
        _prevF11Down = f11Down;

        if (!InGameHasFocus && World == null && InternalServer == null)
        {
            if (Options.MenuMusic)
            {
                SoundManager.PlayRandomMusicIfReady(DefaultMusicCategories.Menu);
            }
            else
            {
                SoundManager.StopMusic(DefaultMusicCategories.Menu);
            }
        }

        Profiler.Start("HUD.update");
        HUD.Update(1.0f);
        Profiler.Stop("HUD.update");

        GameRenderer.UpdateTargetedEntity(1.0F);
        GameRenderer.tick(partialTicks);

        Profiler.Start("chunkProviderLoadOrGenerateSetCurrentChunkOver");
        Profiler.Stop("chunkProviderLoadOrGenerateSetCurrentChunkOver");

        Profiler.Start("playerControllerUpdate");
        if (!IsGamePaused && World != null)
        {
            PlayerController.updateController();
        }
        Profiler.Stop("playerControllerUpdate");

        Profiler.Start("updateDynamicTextures");
        TextureManager.BindTexture(TextureManager.GetTextureId("/terrain.png"));
        if (!IsGamePaused)
        {
            TextureManager.Tick();
        }
        Profiler.Stop("updateDynamicTextures");

        if (CurrentScreen == null && Player != null)
        {
            if (Player.health <= 0)
            {
                Navigate(null);
            }
            else if (Player.isSleeping() && World != null && World.IsRemote)
            {
                Navigate(new SleepScreen(UIContext, Player));
            }
        }
        else if (CurrentScreen is SleepScreen && !Player.isSleeping())
        {
            Navigate(null);
        }

        if (CurrentScreen != null)
        {
            _leftClickCounter = 10000;
            MouseTicksRan = TicksRan + 10000;
        }

        if (CurrentScreen != null)
        {
            CurrentScreen.HandleInput();
            CurrentScreen?.Update(1.0f);
        }

        if (CurrentScreen == null || CurrentScreen.AllowUserInput)
        {
            ProcessInputEvents();
        }

        if (World != null)
        {
            if (Player != null)
            {
                ++_joinPlayerCounter;
                if (_joinPlayerCounter == 30)
                {
                    _joinPlayerCounter = 0;
                    World.Entities.LoadChunksNearEntity(Player);
                }
            }

            World.SetDifficulty(Options.Difficulty);
            InternalServer?.SetDifficulty(Options.Difficulty);

            if (World.IsRemote)
            {
                World.SetDifficulty(3);
            }

            Profiler.Start("entityRendererUpdate");
            if (!IsGamePaused)
            {
                GameRenderer.updateCamera();
            }
            Profiler.Stop("entityRendererUpdate");

            if (!IsGamePaused)
            {
                WorldRenderer.UpdateClouds();
            }

            Profiler.PushGroup("theWorldUpdateEntities");
            if (!IsGamePaused)
            {
                if (World.Environment.LightningTicksLeft > 0)
                {
                    --World.Environment.LightningTicksLeft;
                }
                World.Entities.TickEntities();
            }
            Profiler.PopGroup();

            Profiler.PushGroup("theWorld.tick");
            if (!IsGamePaused || (IsMultiplayerWorld() && InternalServer == null))
            {
                World.allowSpawning(Options.Difficulty > 0, true);
                World.Tick();
            }
            Profiler.PopGroup();

            if (!IsGamePaused && World != null)
            {
                World.displayTick(MathHelper.Floor(Player.x), MathHelper.Floor(Player.y), MathHelper.Floor(Player.z));
            }

            if (!IsGamePaused)
            {
                ParticleManager.updateEffects();
            }
        }

        _systemTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Profiler.PopGroup();
    }

    #endregion

    #region Input Handling

    private void ProcessInputEvents()
    {
        while (Mouse.next())
        {
            long timeSinceLastMouseEvent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _systemTime;

            if (Mouse.getEventDX() != 0 || Mouse.getEventDY() != 0)
            {
                IsControllerMode = false;
                Mouse.setCursorVisible(true);
            }

            if (timeSinceLastMouseEvent <= 200L)
            {
                int mouseWheelDelta = Mouse.getEventDWheel();
                if (mouseWheelDelta != 0)
                {
                    IsControllerMode = false;
                    Mouse.setCursorVisible(true);

                    bool zoomHeld = CurrentScreen == null && InGameHasFocus && Keyboard.isKeyDown(Options.KeyBindZoom.keyCode);
                    if (zoomHeld)
                    {
                        int mouseWheelDirection = mouseWheelDelta > 0 ? 1 : -1;
                        if (mouseWheelDirection > 0)
                        {
                            Options.ZoomScale *= 1.08F;
                        }
                        else
                        {
                            Options.ZoomScale /= 1.08F;
                        }

                        Options.ZoomScale = System.Math.Clamp(Options.ZoomScale, 1.25F, 20.0F);
                    }
                    else
                    {
                        Player.inventory.changeCurrentItem(mouseWheelDelta);
                        if (Options.InvertScrolling)
                        {
                            if (mouseWheelDelta > 0) mouseWheelDelta = 1;
                            if (mouseWheelDelta < 0) mouseWheelDelta = -1;
                            Options.AmountScrolled += (float)mouseWheelDelta * 0.25F;
                        }
                    }
                }

                if (CurrentScreen == null)
                {
                    if (!InGameHasFocus && Mouse.getEventButtonState())
                    {
                        SetIngameFocus();
                    }
                    else
                    {
                        if (Mouse.getEventButton() == 0 && Mouse.getEventButtonState())
                        {
                            ClickMouse(0);
                            MouseTicksRan = TicksRan;
                        }

                        if (Mouse.getEventButton() == 1 && Mouse.getEventButtonState())
                        {
                            ClickMouse(1);
                            MouseTicksRan = TicksRan;
                        }

                        if (Mouse.getEventButton() == 2 && Mouse.getEventButtonState())
                        {
                            ClickMiddleMouseButton();
                        }
                    }
                }
                else
                {
                    CurrentScreen?.HandleMouseInput();
                }
            }
        }

        if (_leftClickCounter > 0)
        {
            --_leftClickCounter;
        }

        while (Keyboard.Next())
        {
            Player?.handleKeyPress(Keyboard.getEventKey(), Keyboard.getEventKeyState());

            if (Keyboard.getEventKeyState())
            {
                if (CurrentScreen != null)
                {
                    CurrentScreen.HandleKeyboardInput();
                }
                else
                {
                    if (Keyboard.getEventKey() == Keyboard.KEY_ESCAPE) DisplayInGameMenu();

                    if (Keyboard.getEventKey() == Keyboard.KEY_S && Keyboard.isKeyDown(Keyboard.KEY_F3))
                    {
                        ForceReload();
                    }

                    if (Keyboard.getEventKey() == Keyboard.KEY_H && Keyboard.isKeyDown(Keyboard.KEY_F3))
                    {
                        Options.AdvancedItemTooltips = !Options.AdvancedItemTooltips;
                        Options.SaveOptions();
                    }

                    if (Keyboard.getEventKey() == Keyboard.KEY_D && Keyboard.isKeyDown(Keyboard.KEY_F3))
                    {
                        HUD.Chat.ClearMessages();
                    }

                    if (Keyboard.getEventKey() == Keyboard.KEY_C && Keyboard.isKeyDown(Keyboard.KEY_F3))
                    {
                        throw new Exception("Simulated crash triggered by pressing F3 + C");
                    }

                    if (Keyboard.getEventKey() == Keyboard.KEY_F1) Options.HideGUI = !Options.HideGUI;

                    if (Keyboard.getEventKey() == Keyboard.KEY_F3)
                    {
                        if (Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT))
                        {
                            Navigate(new DebugEditorScreen(UIContext, null, DebugComponentsStorage));
                        }
                        else
                        {
                            Options.ShowDebugInfo = !Options.ShowDebugInfo;
                        }
                    }

                    if (Keyboard.getEventKey() == Keyboard.KEY_F5)
                    {
                        Options.CameraMode = (EnumCameraMode)((int)(Options.CameraMode + 2) % 3);
                    }

                    if (Keyboard.getEventKey() == Keyboard.KEY_F8) Options.SmoothCamera = !Options.SmoothCamera;
                    if (Keyboard.getEventKey() == Keyboard.KEY_F7) ShowChunkBorders = !ShowChunkBorders;

                    if (Keyboard.getEventKey() == Options.KeyBindInventory.keyCode)
                    {
                        Navigate(new InventoryScreen(UIContext, Player, PlayerController, () => CurrentScreen));
                    }

                    if (Keyboard.getEventKey() == Options.KeyBindDrop.keyCode) Player.DropSelectedItem();

                    if (Keyboard.getEventKey() == Options.KeyBindChat.keyCode)
                    {
                        Navigate(new ChatScreen(UIContext, HUD.Chat, Player));
                    }

                    if (Keyboard.getEventKey() == Options.KeyBindCommand.keyCode)
                    {
                        Navigate(new ChatScreen(UIContext, HUD.Chat, Player, "/"));
                    }
                }

                for (int slotIndex = 0; slotIndex < 9; ++slotIndex)
                {
                    if (Keyboard.getEventKey() == Keyboard.KEY_1 + slotIndex)
                    {
                        Player.inventory.selectedSlot = slotIndex;
                    }
                }

                if (Keyboard.getEventKey() == Options.KeyBindToggleFog.keyCode)
                {
                    Options.RenderDistanceOption.Value = System.Math.Clamp(
                        Options.RenderDistanceOption.Value + (!Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) && !Keyboard.isKeyDown(Keyboard.KEY_RSHIFT) ? 1.0f / 28.0f : -1.0f / 28.0f),
                        0.0f,
                        1.0f);
                }
            }
        }

        ControllerManager.UpdateGui(CurrentScreen);
        ControllerManager.UpdateInGame(Timer.renderPartialTicks);

        if (CurrentScreen == null)
        {
            if (Mouse.isButtonDown(0) && (float)(TicksRan - MouseTicksRan) >= Timer.ticksPerSecond / 4.0F && InGameHasFocus)
            {
                ClickMouse(0);
                MouseTicksRan = TicksRan;
            }

            if (Mouse.isButtonDown(1) && (float)(TicksRan - MouseTicksRan) >= Timer.ticksPerSecond / 4.0F && InGameHasFocus)
            {
                ClickMouse(1);
                MouseTicksRan = TicksRan;
            }
        }

        UpdateHeldMouseButton(0, CurrentScreen == null && (Mouse.isButtonDown(0) || Controller.RightTrigger > 0.5f) && InGameHasFocus);
    }

    public void ClickMouse(int mouseButton)
    {
        if (mouseButton != 0 || _leftClickCounter <= 0)
        {
            if (mouseButton == 0)
            {
                Player.swingHand();
            }

            bool shouldPerformSecondaryAction = true;
            if (ObjectMouseOver.Type == HitResultType.MISS)
            {
                if (mouseButton == 0)
                {
                    _leftClickCounter = 10;
                }
            }
            else if (ObjectMouseOver.Type == HitResultType.ENTITY)
            {
                if (mouseButton == 0)
                {
                    PlayerController.attackEntity(Player, ObjectMouseOver.Entity);
                }

                if (mouseButton == 1)
                {
                    PlayerController.interactWithEntity(Player, ObjectMouseOver.Entity);
                }
            }
            else if (ObjectMouseOver.Type == HitResultType.TILE)
            {
                int blockX = ObjectMouseOver.BlockX;
                int blockY = ObjectMouseOver.BlockY;
                int blockZ = ObjectMouseOver.BlockZ;
                int blockSide = ObjectMouseOver.Side;
                if (mouseButton == 0)
                {
                    PlayerController.clickBlock(blockX, blockY, blockZ, ObjectMouseOver.Side);
                }
                else
                {
                    ItemStack selectedItem = Player.inventory.getSelectedItem();
                    int itemCountBefore = selectedItem != null ? selectedItem.count : 0;
                    if (PlayerController.sendPlaceBlock(Player, World, selectedItem, blockX, blockY, blockZ, blockSide))
                    {
                        shouldPerformSecondaryAction = false;
                        Player.swingHand();
                    }

                    if (selectedItem == null)
                    {
                        return;
                    }

                    if (selectedItem.count == 0)
                    {
                        Player.inventory.main[Player.inventory.selectedSlot] = null;
                    }
                    else if (selectedItem.count != itemCountBefore)
                    {
                        GameRenderer.itemRenderer.ResetEquippedProgress();
                    }
                }
            }

            if (shouldPerformSecondaryAction && mouseButton == 1)
            {
                ItemStack selectedItem = Player.inventory.getSelectedItem();
                if (selectedItem != null && PlayerController.sendUseItem(Player, World, selectedItem))
                {
                    GameRenderer.itemRenderer.ResetEquippedProgress();
                }
            }
        }
    }

    public void ClickMiddleMouseButton()
    {
        if (ObjectMouseOver.Type != HitResultType.MISS)
        {
            int blockId = World.Reader.GetBlockId(ObjectMouseOver.BlockX, ObjectMouseOver.BlockY, ObjectMouseOver.BlockZ);

            if (blockId == Block.GrassBlock.id) blockId = Block.Dirt.id;
            if (blockId == Block.DoubleSlab.id) blockId = Block.Slab.id;
            if (blockId == Block.Bedrock.id) blockId = Block.Stone.id;

            Player.inventory.setCurrentItem(blockId, false);
        }
    }

    private void UpdateHeldMouseButton(int mouseButton, bool isHoldingMouse)
    {
        if (!PlayerController.IsTestPlayer)
        {
            if (!isHoldingMouse)
            {
                _leftClickCounter = 0;
            }

            if (mouseButton != 0 || _leftClickCounter <= 0)
            {
                if (isHoldingMouse && ObjectMouseOver.Type != HitResultType.MISS && ObjectMouseOver.Type == HitResultType.TILE &&
                    mouseButton == 0)
                {
                    int blockX = ObjectMouseOver.BlockX;
                    int blockY = ObjectMouseOver.BlockY;
                    int blockZ = ObjectMouseOver.BlockZ;
                    PlayerController.sendBlockRemoving(blockX, blockY, blockZ, ObjectMouseOver.Side);
                    ParticleManager.addBlockHitEffects(blockX, blockY, blockZ, ObjectMouseOver.Side);
                }
                else
                {
                    PlayerController.resetBlockRemoving();
                }
            }
        }
    }

    #endregion

    #region World & Server Operations

    public void LoadWorld(string dir, string displayName, WorldSettings settings)
    {
        StatFileWriter.ReadStat(Stats.Stats.LoadWorldStat, 1);
        PlayerController = new PlayerControllerSP(this);
        StartWorld(dir, displayName, settings);
    }

    public void StartWorld(string worldName, string mainMenuText, WorldSettings settings)
    {
        ChangeWorld(null);
        Navigate(new LevelLoadingScreen(UIContext, CreateNetworkContext(), worldName, settings, this));
    }

    public void ChangeWorld(World? newWorld, string loadingText = "", EntityPlayer? targetEntity = null)
    {
        StatFileWriter.Tick();
        StatFileWriter.SyncStats();
        _loadingScreen.printText(loadingText);
        _loadingScreen.progressStage("");
        SoundManager.PlayStreaming(null!, 0.0F, 0.0F, 0.0F, 0.0F, 0.0F);

        World = newWorld!;
        if (newWorld != null)
        {
            PlayerController.ChangeWorld(newWorld);
            if (!IsMultiplayerWorld())
            {
                if (targetEntity == null)
                {
                    Player = (ClientPlayerEntity?)newWorld.GetPlayerForProxy(typeof(ClientPlayerEntity));
                }
            }
            else if (Player != null)
            {
                Player.teleportToTop();
                newWorld?.Entities.SpawnEntity(Player);
            }

            if (Player == null)
            {
                Player = (ClientPlayerEntity)PlayerController.createPlayer(newWorld);
                Player.teleportToTop();
                PlayerController.flipPlayer(Player);
            }

            Player.movementInput = new MovementInputFromOptions(Options);
            WorldRenderer?.ChangeWorld(newWorld);
            ParticleManager?.clearEffects(newWorld);

            PlayerController.fillHotbar(Player);
            if (targetEntity != null)
            {
                newWorld.SaveWorldData();
            }

            newWorld.AddPlayer(Player);
            SkinManager.RequestDownload(Player.name);

            if (newWorld.IsNewWorld)
            {
                newWorld.SavingProgress(_loadingScreen);
            }
        }
        else
        {
            Player = null;
        }

        _systemTime = 0L;
    }

    public void Respawn(bool ignoreSpawnPosition, int newDimensionId)
    {
        Vec3i? playerSpawnPos = null;
        Vec3i? respawnPos = null;

        if (Player is not null && !ignoreSpawnPosition)
        {
            playerSpawnPos = Player.getSpawnPos();

            if (playerSpawnPos is not null)
            {
                respawnPos = EntityPlayer.findRespawnPosition(World, playerSpawnPos);

                if (respawnPos is null)
                {
                    Player.sendMessage("tile.bed.notValid");
                }
            }
        }

        bool useBedSpawn = respawnPos is not null;
        Vec3i finalRespawnPos = respawnPos ?? World.Properties.GetSpawnPos();

        World.UpdateSpawnPosition();
        World.Entities.UpdateEntityLists();

        int previousPlayerId = 0;

        if (Player is not null)
        {
            previousPlayerId = Player.id;
            World.Entities.Remove(Player);
        }

        Player = (ClientPlayerEntity)PlayerController.createPlayer(World);
        Player.dimensionId = newDimensionId;
        Player.teleportToTop();

        if (useBedSpawn)
        {
            Player.setSpawnPos(playerSpawnPos);
            Player.setPositionAndAnglesKeepPrevAngles(
                finalRespawnPos.X + 0.5,
                finalRespawnPos.Y + 0.1,
                finalRespawnPos.Z + 0.5,
                0.0F,
                0.0F);
        }

        PlayerController.flipPlayer(Player);
        World.AddPlayer(Player);
        Player.movementInput = new MovementInputFromOptions(Options);
        Player.id = previousPlayerId;
        Player.spawn();
        PlayerController.fillHotbar(Player);

        ShowText("Respawning");

        if (_isGameOverOpen)
        {
            Navigate(null);
        }
    }

    public void StartInternalServer(string worldDir, WorldSettings worldSettings)
    {
        InternalServer = new InternalServer(Path.Combine(BetaSharpDir, "saves"), worldDir, worldSettings, Options.renderDistance, Options.Difficulty);
        InternalServer.RunThreaded("Internal Server");
    }

    public void StopInternalServer()
    {
        if (InternalServer != null)
        {
            InternalServer.Stop();
            while (!InternalServer.stopped)
            {
                Thread.Sleep(1);
            }

            InternalServer = null;
        }
    }

    public bool IsMultiplayerWorld()
    {
        return World != null && World.IsRemote;
    }

    private void ShowText(string loadingText)
    {
        _loadingScreen.printText(loadingText);
        _loadingScreen.progressStage("Building terrain");
        short loadingRadius = 128;
        int loadedChunkCount = 0;
        int totalChunksToLoad = loadingRadius * 2 / 16 + 1;
        totalChunksToLoad *= totalChunksToLoad;
        Vec3i centerPos = World.Properties.GetSpawnPos();

        if (Player != null)
        {
            centerPos.X = (int)Player.x;
            centerPos.Z = (int)Player.z;
        }

        for (int xOffset = -loadingRadius; xOffset <= loadingRadius; xOffset += 16)
        {
            for (int zOffset = -loadingRadius; zOffset <= loadingRadius; zOffset += 16)
            {
                _loadingScreen.setLoadingProgress(loadedChunkCount++ * 100 / totalChunksToLoad);
                World.Reader.GetBlockId(centerPos.X + xOffset, 64, centerPos.Z + zOffset);

                while (World.Lighting.DoLightingUpdates())
                {
                }
            }
        }

        _loadingScreen.progressStage("Simulating world for a bit");
        World.TickChunks();
    }

    #endregion

    #region UI & Navigation

    public void Navigate(UIScreen? newScreen)
    {
        Mouse.ClearEvents();
        Controller.ClearEvents();
        CurrentScreen?.Uninit();

        if (newScreen is MainMenuScreen)
        {
            StatFileWriter.Tick();

            if (InGameHasFocus)
            {
                SoundManager.StopCurrentMusic();
            }
        }

        StatFileWriter.SyncStats();
        if (newScreen == null && World == null)
        {
            newScreen = CreateMainMenuScreen();
        }
        else if (newScreen == null && Player.health <= 0)
        {
            newScreen = new GameOverScreen(UIContext, (int)Player.getScore(), Player.respawn, canRespawn: Session != null, exitToTitle: () => ChangeWorld(null!));
        }

        if (newScreen is MainMenuScreen)
        {
            HUD.Chat.ClearMessages();
        }

        CurrentScreen = newScreen;

        if (CurrentScreen != null)
        {
            VirtualCursor.Reset(DisplayWidth, DisplayHeight);
        }

        if (InternalServer != null)
        {
            bool shouldPause = newScreen?.PausesGame ?? false;
            InternalServer.SetPaused(shouldPause);
        }

        if (newScreen != null)
        {
            SetIngameNotInFocus();
            newScreen.Initialize();
            SkipRenderWorld = false;
        }
        else
        {
            SetIngameFocus();
            SoundManager.StopMusic(DefaultMusicCategories.Menu);
        }
    }

    public void DisplayInGameMenu()
    {
        if (CurrentScreen == null)
        {
            bool isMP = IsMultiplayerWorld() && InternalServer == null;
            string quitText = isMP ? "Disconnect" : "Save and quit to title";
            int saveStep = 0;
            Navigate(new IngameMenuScreen(UIContext, StatFileWriter, DebugComponentsStorage, SetIngameFocus, quitText, () =>
            {
                if (IsMultiplayerWorld()) World.Disconnect();
                StopInternalServer();
                ChangeWorld(null);
                Options.ShowDebugInfo = false;
            }, () => World?.AttemptSaving(saveStep++) ?? false));
        }
    }

    public void SetIngameFocus()
    {
        if (Display.isActive())
        {
            if (!InGameHasFocus)
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                InGameHasFocus = true;
                MouseHelper.grabMouseCursor();
                Navigate(null);
                _leftClickCounter = 10000;
                MouseTicksRan = TicksRan + 10000;
            }
        }
    }

    private void SetIngameNotInFocus()
    {
        if (InGameHasFocus)
        {
            Player?.resetPlayerKeyState();
            InGameHasFocus = false;
            GCSettings.LatencyMode = GCLatencyMode.Batch;
            MouseHelper.ungrabMouseCursor();
            Mouse.setCursorVisible(!IsControllerMode);
        }
    }

    private MainMenuScreen CreateMainMenuScreen() => new(UIContext, Session, _hideQuitButton, this, CreateNetworkContext(), TexturePackList, DebugComponentsStorage, Shutdown);
    private ClientNetworkContext CreateNetworkContext() => new(this, this, this, Session, StatFileWriter, ParticleManager, HUD.AddChatMessage, this);

    #endregion

    #region System Utilities

    public void ToggleFullscreen()
    {
        try
        {
            _fullscreen = !_fullscreen;
            if (_fullscreen)
            {
                _tempDisplayWidth = DisplayWidth;
                _tempDisplayHeight = DisplayHeight;

                Display.setDisplayMode(Display.getDesktopDisplayMode());
                Display.setFullscreen(true);
                DisplayWidth = Display.getDisplayMode().getWidth();
                DisplayHeight = Display.getDisplayMode().getHeight();

                if (DisplayWidth <= 0) DisplayWidth = 1;
                if (DisplayHeight <= 0) DisplayHeight = 1;
            }
            else
            {
                Display.setFullscreen(false);
                if (_tempDisplayWidth > 0 && _tempDisplayHeight > 0)
                {
                    Display.setDisplayMode(new DisplayMode(_tempDisplayWidth, _tempDisplayHeight));
                    DisplayWidth = _tempDisplayWidth;
                    DisplayHeight = _tempDisplayHeight;
                }
                else
                {
                    Display.setDisplayMode(new DisplayMode(854, 480));
                    DisplayWidth = 854;
                    DisplayHeight = 480;
                }

                if (DisplayWidth <= 0) DisplayWidth = 1;
                if (DisplayHeight <= 0) DisplayHeight = 1;

                // Center the window
                DisplayMode desktopMode = Display.getDesktopDisplayMode();
                int centerX = (desktopMode.getWidth() - DisplayWidth) / 2;
                int centerY = (desktopMode.getHeight() - DisplayHeight) / 2;
                Display.setLocation(centerX, centerY);
            }

            Resize(DisplayWidth, DisplayHeight);
            Display.update();
        }
        catch (Exception displayException)
        {
            _logger.LogError(displayException.ToString());
        }
    }

    private void Resize(int newWidth, int newHeight)
    {
        if (newWidth <= 0) newWidth = 1;
        if (newHeight <= 0) newHeight = 1;

        DisplayWidth = newWidth;
        DisplayHeight = newHeight;
        Mouse.setDisplayDimensions(DisplayWidth, DisplayHeight);

        PostProcessManager.Resize(Display.getFramebufferWidth(), Display.getFramebufferHeight());
    }

    private void ScreenshotListener()
    {
        if (Keyboard.isKeyDown(Keyboard.KEY_F2))
        {
            if (!_isTakingScreenshot)
            {
                _isTakingScreenshot = true;
                int framebufferWidth = Display.getFramebufferWidth();
                int framebufferHeight = Display.getFramebufferHeight();
                int size = framebufferWidth * framebufferHeight * 3;
                byte[] pixels = new byte[size];
                GLManager.GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                unsafe
                {
                    fixed (byte* p = pixels)
                    {
                        GLManager.GL.ReadPixels(0, 0, (uint)framebufferWidth, (uint)framebufferHeight, PixelFormat.Rgb, PixelType.UnsignedByte, p);
                    }
                }

                string result = ScreenShotHelper.saveScreenshot(_gameDataDir, DisplayWidth, DisplayHeight, pixels);
                HUD.AddChatMessage(result);
            }
        }
        else
        {
            _isTakingScreenshot = false;
        }
    }

    private void ForceReload()
    {
        _logger.LogInformation("FORCING RELOAD!");
        SoundManager = new SoundManager();
        SoundManager.LoadSoundSettings(Options);
        DefaultMusicCategories.Register(SoundManager);
    }

    public void InstallResource(string resourcePath, FileInfo resourceFile)
    {
        if (!resourceFile.FullName.EndsWith("ogg"))
        {
            //TODO: ADD SUPPORT FOR MUS SFX?
            return;
        }

        int slashIndex = resourcePath.IndexOf("/");
        string category = resourcePath.Substring(0, slashIndex);
        resourcePath = resourcePath.Substring(slashIndex + 1);

        if (category.Equals("sound", StringComparison.OrdinalIgnoreCase))
        {
            SoundManager.AddSound(resourcePath, resourceFile);
        }
        else if (category.Equals("newsound", StringComparison.OrdinalIgnoreCase))
        {
            SoundManager.AddSound(resourcePath, resourceFile);
        }
        else if (category.Equals("streaming", StringComparison.OrdinalIgnoreCase))
        {
            SoundManager.AddStreaming(resourcePath, resourceFile);
        }
        else if (category.Equals("music", StringComparison.OrdinalIgnoreCase))
        {
            SoundManager.AddMusic(DefaultMusicCategories.Game, resourcePath, resourceFile);
        }
        else if (category.Equals("newmusic", StringComparison.OrdinalIgnoreCase))
        {
            SoundManager.AddMusic(DefaultMusicCategories.Game, resourcePath, resourceFile);
        }
        else if (category.Equals("custom", StringComparison.OrdinalIgnoreCase))
        {
            int subSlash = resourcePath.IndexOf("/");
            string subCategory = resourcePath.Substring(0, subSlash);
            resourcePath = resourcePath.Substring(subSlash + 1);

            if (subCategory.Equals("music", StringComparison.OrdinalIgnoreCase))
            {
                SoundManager.AddMusic(DefaultMusicCategories.Menu, resourcePath, resourceFile);
            }
        }
    }

    public string WorldDebugInfo => World.GetDebugInfo();
    public string ParticleDebugInfo => "Particles: " + ParticleManager.getStatistics();
    internal DebugSystemSnapshot DebugSystemSnapshot => _debugTelemetry.SystemSnapshot;

    [Conditional("DEBUG")]
    private void CheckGLError(string location)
    {
        GLEnum glError = GLManager.GL.GetError();
        if (glError != 0)
        {
            _logger.LogError($"#### GL ERROR ####");
            _logger.LogError($"@ {location}");
            _logger.LogError($"> {glError.ToString()}");
            _logger.LogError($"");
        }
    }

    private void LoadScreen()
    {
        ScaledResolution var1 = new(Options, DisplayWidth, DisplayHeight);
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0D, var1.ScaledWidth, var1.ScaledHeight, 0.0D, 1000.0D, 3000.0D);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Translate(0.0F, 0.0F, -2000.0F);
        GLManager.GL.Viewport(0, 0, (uint)Display.getFramebufferWidth(), (uint)Display.getFramebufferHeight());
        GLManager.GL.ClearColor(0.0F, 0.0F, 0.0F, 0.0F);
        Tessellator tessellator = Tessellator.instance;
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Fog);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        TextureManager.BindTexture(TextureManager.GetTextureId("/title/mojang.png"));
        tessellator.startDrawingQuads();
        tessellator.setColorOpaque_I(0xFFFFFF);
        tessellator.addVertexWithUV(0.0D, (double)DisplayHeight, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV((double)DisplayWidth, (double)DisplayHeight, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV((double)DisplayWidth, 0.0D, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV(0.0D, 0.0D, 0.0D, 0.0D, 0.0D);
        tessellator.draw();
        short var3 = 256;
        short var4 = 256;
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        tessellator.setColorOpaque_I(0xFFFFFF);
        DrawTextureRegion((var1.ScaledWidth - var3) / 2, (var1.ScaledHeight - var4) / 2, 0, 0, var3, var4);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.Fog);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.AlphaFunc(GLEnum.Greater, 0.1F);
        Display.swapBuffers();
    }

    private static void DrawTextureRegion(int x, int y, int texX, int texY, int width, int height)
    {
        const float uScale = 1 / 256f;
        const float vScale = 1 / 256f;

        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(x + 0, y + height, 0, (texX + 0) * uScale, (texY + height) * vScale);
        tess.addVertexWithUV(x + width, y + height, 0, (texX + width) * uScale, (texY + height) * vScale);
        tess.addVertexWithUV(x + width, y + 0, 0, (texX + width) * uScale, (texY + 0) * vScale);
        tess.addVertexWithUV(x + 0, y + 0, 0, (texX + 0) * uScale, (texY + 0) * vScale);
        tess.draw();
    }

    #endregion

    #region OS Interop

    [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static partial uint TimeBeginPeriod(uint period);

    [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static partial uint TimeEndPeriod(uint period);

    public void InitializeTimer()
    {
        if (s_isWindows) TimeBeginPeriod(1);
    }

    public void CleanupTimer()
    {
        if (s_isWindows) TimeEndPeriod(1);
    }

    #endregion

    #region Application Entry Point

    public static void Startup(string[] args)
    {
        (string Name, string Session) result = args.Length switch
        {
            0 => ($"Player{Random.Shared.Next()}", "-"),
            1 => (args[0], "-"),
            _ => (args[0], args[1]),
        };

        StartMainThread(result.Name, result.Session);
    }

    private static void StartMainThread(string playerName, string sessionToken)
    {
        Thread.CurrentThread.Name = "BetaSharp Main Thread";

        BetaSharp game = new(850, 480, false);

        if (playerName != null && sessionToken != null)
        {
            game.Session = new Session(playerName, sessionToken);

            if (sessionToken == "-")
            {
                HasPaidCheckTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
        else
        {
            throw new Exception("Player name and session token were not provided!");
        }

        game.Run();
    }

    #endregion
}
