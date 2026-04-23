using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using BetaSharp.Blocks;
using BetaSharp.Client.Achievements;
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
using BetaSharp.Client.Resource;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.Sound;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Screens;
using BetaSharp.Client.UI.Screens.InGame;
using BetaSharp.Client.UI.Screens.InGame.Containers;
using BetaSharp.Client.UI.Screens.Menu;
using BetaSharp.Client.UI.Screens.Menu.Net;
using BetaSharp.Diagnostics;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Profiling;
using BetaSharp.Registries;
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
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
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
    public RegistryAccess RegistryAccess { get; private set; } = RegistryAccess.Empty;

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

    /// <summary>
    /// When the debug viewport is active, the top-left pixel offset of the game viewport
    /// within the window.
    /// </summary>
    public Vector2 DebugViewportOffset { get; private set; }

    /// <summary>
    /// The top-left screen position of the game viewport in ImGui/window pixels.
    /// Zero when the debug menu is closed.
    /// </summary>
    public Vector2 DebugViewportScreenPos => _debugWindowManager?.ViewportPos ?? Vector2.Zero;

    public bool ShowChunkBorders { get; private set; }
    public bool SkipRenderWorld { get; private set; }
    public string DebugText { get; private set; } = "";
    public HitResult ObjectMouseOver = new(HitResultType.MISS);

    public GameRenderer GameRenderer { get; private set; }
    public WorldRenderer WorldRenderer { get; private set; }
    public FramebufferManager FramebufferManager { get; private set; }
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


    private DebugWindowManager _debugWindowManager;
    private GLErrorHandler _glErrorHandler;
    private string _gameDataDir;

    private bool _fullscreen;
    private bool _prevF11Down;
    private bool _prevF3Down;
    private Vector2 _lastViewportSize;

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
        MetricRegistry.Bootstrap(typeof(ClientMetrics));
        MetricRegistry.Bootstrap(typeof(RenderMetrics));

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

        Profiler.RegisterMainThread();

        try
        {
            int[] msaaValues = [0, 2, 4, 8];
            Display.MSAA_Samples = msaaValues[Options.MSAALevel];

            Display.create();
            Display.getGlfw().SetWindowSizeLimits(Display.GetWindowHandle(), 850, 480, maximumWidth, maximumHeight);

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

#if DEBUG
            _glErrorHandler = new();
#endif
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
            inputDisplaySize: () =>
            {
                if (Options.ShowDebugInfo && _debugWindowManager != null)
                {
                    Vector2 vs = _debugWindowManager.ViewportSize;
                    if (vs.X > 0 && vs.Y > 0)
                        return new Vector2D<int>((int)vs.X, (int)vs.Y);
                }
                return new Vector2D<int>(DisplayWidth, DisplayHeight);
            },
            controllerState: this,
            VirtualCursor,
            Timer,
            navigator: this,
            hasWorld: () => World != null,
            mouseOffset: () => new Vector2D<int>((int)DebugViewportOffset.X, (int)DebugViewportOffset.Y)
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

        ImGui.CreateContext();

        // ImGuiImplGLFW and ImGuiImplOpenGL3 are compiled into separate native DLLs,
        // each with their own GImGui context pointer. We must share the context created
        // by cimgui.dll with both backend DLLs before calling their Init functions.
        ImGuiImplGLFW.SetCurrentContext(ImGui.GetCurrentContext());
        ImGuiImplOpenGL3.SetCurrentContext(ImGui.GetCurrentContext());

        ImGuiIO* io = ImGui.GetIO();
        io->ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;

        // Install game input callbacks first so the ImGui GLFW backend can chain to them.
        Keyboard.create(Display.getGlfw(), Display.GetWindowHandle());
        Mouse.create(Display.getGlfw(), Display.GetWindowHandle(), Display.getWidth(), Display.getHeight());
        Controller.Create(Display.getGlfw(), Display.GetWindowHandle());

        ImGuiImplGLFW.InitForOpenGL((GLFWwindow*)Display.GetWindowHandle(), true);
        ImGuiImplOpenGL3.Init("#version 330 core");
        DebugWindowManager.ApplyStyle();

        _debugWindowManager = new DebugWindowManager(this, () => InGameHasFocus);

        ControllerManager.Initialize(this);
        MouseHelper = new MouseHelper
        {
            GetUngrabCenter = () =>
            {
                if (Options.ShowDebugInfo && _debugWindowManager != null)
                {
                    Vector2 vp = _debugWindowManager.ViewportPos;
                    Vector2 vs = _debugWindowManager.ViewportSize;
                    if (vs.X > 0 && vs.Y > 0)
                    {
                        return new((int)(vp.X + vs.X / 2), (int)(vp.Y + vs.Y / 2));
                    }
                }
                return new(Display.getWidth() / 2, Display.getHeight() / 2);
            }
        };

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
        RegistryAccess = RegistryAccess.Build();

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
            () => CurrentScreen == null && Player != null && World != null
                ? new InGameTipContext(ObjectMouseOver, World.Reader, Player.inventory.GetItemInHand())
                : null,
            () => _isMainMenuOpen
        ));

        FramebufferManager = new FramebufferManager(Display.getFramebufferWidth(), Display.getFramebufferHeight(), Options);

        EntityRenderDispatcher.Instance.SkinManager.RequestDownload(Session.username, true);
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

            // don't bother trying to shutdown imgui because it keeps hanging/crashing

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

                Profiler.Update(Timer.DeltaTime);

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
                        Vector2D<int> inputSize = UIContext.InputDisplaySize;
                        VirtualCursor.Update(CurrentScreen, Options, inputSize.X, inputSize.Y, Timer.DeltaTime);
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

                    bool imguiThisFrame = Options.ShowDebugInfo;
                    if (imguiThisFrame)
                    {
                        ImGuiImplOpenGL3.NewFrame();
                        ImGuiImplGLFW.NewFrame();
                        ImGui.NewFrame();
                        ImGuiInput.CapturingKeyboard = ImGui.GetIO().WantCaptureKeyboard && !_debugWindowManager.GameViewportFocused;
                    }
                    else
                    {
                        ImGuiInput.CapturingKeyboard = false;
                    }

                    long tickStartTime = Stopwatch.GetTimestamp();

                    using (Profiler.Begin("Ticks"))
                    {
                        for (int tickIndex = 0; tickIndex < Timer.elapsedTicks; ++tickIndex)
                        {
                            ++TicksRan;
                            RunTick(Timer.renderPartialTicks);
                        }
                    }

                    long tickElapsedTime = Stopwatch.GetTimestamp() - tickStartTime;
                    CheckGLError("Pre render");

                    SoundManager.UpdateListener(Player, Timer.renderPartialTicks);
                    GLManager.GL.Enable(GLEnum.Texture2D);

                    if (World != null)
                    {
                        using (Profiler.Begin("UpdateLighting"))
                        {
                            World.Lighting.DoLightingUpdates();
                        }
                    }

                    if (!Keyboard.isKeyDown(Keyboard.KEY_F7))
                    {
                        using (Profiler.Begin("DisplayPresent"))
                        {
                            Display.update();
                        }
                    }

                    if (Player != null && Player.IsInsideWall())
                    {
                        Options.CameraMode = CameraMode.FirstPerson;
                    }

                    int savedWidth = DisplayWidth, savedHeight = DisplayHeight;
                    if (imguiThisFrame)
                    {
                        Vector2 vpSize = _debugWindowManager.ViewportSize;
                        if (vpSize.X > 0 && vpSize.Y > 0)
                        {
                            int vpW = (int)vpSize.X, vpH = (int)vpSize.Y;
                            if (_lastViewportSize != vpSize)
                            {
                                FramebufferManager.Resize(vpW, vpH);
                                _lastViewportSize = vpSize;
                            }
                            DisplayWidth = vpW;
                            DisplayHeight = vpH;

                            DebugViewportOffset = new Vector2(
                                _debugWindowManager.ViewportPos.X,
                                Display.getHeight() - vpH - _debugWindowManager.ViewportPos.Y);
                            FramebufferManager.SkipBlit = true;
                        }
                        else
                        {
                            DebugViewportOffset = Vector2.Zero;
                            FramebufferManager.SkipBlit = false;
                        }
                    }
                    else
                    {
                        DebugViewportOffset = Vector2.Zero;
                        FramebufferManager.SkipBlit = false;
                        if (_lastViewportSize != Vector2.Zero)
                        {
                            FramebufferManager.Resize(Display.getFramebufferWidth(), Display.getFramebufferHeight());
                            _lastViewportSize = Vector2.Zero;
                            _debugWindowManager.ViewportTextureId = 0;
                        }
                    }

                    if (!SkipRenderWorld)
                    {
                        PlayerController?.setPartialTime(Timer.renderPartialTicks);

                        TextureStats.StartFrame();

                        using (Profiler.Begin("Render"))
                        {
                            GameRenderer.onFrameUpdate(Timer.renderPartialTicks);
                        }

                        TextureStats.EndFrame();
                        PushRenderMetrics();
                    }

                    DisplayWidth = savedWidth;
                    DisplayHeight = savedHeight;

                    if (imguiThisFrame)
                    {
                        if (FramebufferManager.SkipBlit)
                        {
                            _debugWindowManager.ViewportTextureId = FramebufferManager.TextureId;
                        }

                        using (Profiler.Begin("ImguiBuild"))
                        {
                            _debugWindowManager.Render(Timer.DeltaTime);
                        }

                        using (Profiler.Begin("ImguiSubmit"))
                        {
                            ImGui.Render();
                            ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
                        }
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
                        MetricRegistry.Set(ClientMetrics.Fps, frameCounter);
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
                    ReportFrameTelemetry(frameStartNano);
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

    private void PushRenderMetrics()
    {
        if (WorldRenderer?.ChunkRenderer is not { } cr) return;
        MetricRegistry.Set(RenderMetrics.ChunksTotal, cr.TotalChunks);
        MetricRegistry.Set(RenderMetrics.ChunksFrustum, cr.ChunksInFrustum);
        MetricRegistry.Set(RenderMetrics.ChunksOccluded, cr.ChunksOccluded);
        MetricRegistry.Set(RenderMetrics.ChunksRendered, cr.ChunksRendered);
        MetricRegistry.Set(RenderMetrics.VboAllocatedMb, (float)(VertexBuffer<ChunkVertex>.Allocated / 1_000_000.0));
        MetricRegistry.Set(RenderMetrics.MeshVersionAllocated, ChunkMeshVersion.TotalAllocated);
        MetricRegistry.Set(RenderMetrics.MeshVersionReleased, ChunkMeshVersion.TotalReleased);
        MetricRegistry.Set(RenderMetrics.TextureBindsLastFrame, TextureStats.BindsLastFrame);
        MetricRegistry.Set(RenderMetrics.TextureAvgBinds, (float)TextureStats.AverageBindsPerFrame);
        MetricRegistry.Set(RenderMetrics.TextureActive, GLTexture.ActiveTextureCount);
        MetricRegistry.Set(RenderMetrics.EntitiesRendered, WorldRenderer.CountEntitiesRendered);
        MetricRegistry.Set(RenderMetrics.EntitiesHidden, WorldRenderer.CountEntitiesHidden);
        MetricRegistry.Set(RenderMetrics.EntitiesTotal, WorldRenderer.CountEntitiesTotal);
        MetricRegistry.Set(RenderMetrics.ParticlesActive, ParticleManager.ActiveParticleCount);
    }

    private void ReportFrameTelemetry(long frameStartNano)
    {
        long frameEndNano = Stopwatch.GetTimestamp();
        double thisFrameTimeMs = (frameEndNano - frameStartNano) / 1000000.0;
        _debugTelemetry.RecordFrameTime(thisFrameTimeMs);
        MetricRegistry.Set(ClientMetrics.FrameTimeMs, (float)thisFrameTimeMs);

        Profiler.Record("FrameTime", thisFrameTimeMs);
        Profiler.CaptureFrame();
    }

    #endregion

    #region Tick Logic

    public void RunTick(float partialTicks)
    {
        using Profiler.ProfilerScope _tick = Profiler.Begin("Tick");

        using (Profiler.Begin("SyncStats"))
        {
            StatFileWriter.SyncStatsIfReady();
        }

        bool f11Down = Keyboard.isKeyDown(Keyboard.KEY_F11);
        if (f11Down && !_prevF11Down)
        {
            ToggleFullscreen();
        }
        _prevF11Down = f11Down;

        // F3 uses edge detection so it works even when
        // CurrentScreen.HandleInput() has already consumed all keyboard events.
        bool f3Down = Keyboard.isKeyDown(Keyboard.KEY_F3);
        if (f3Down && !_prevF3Down && !ImGuiInput.CapturingKeyboard)
        {
            Options.ShowDebugInfo = !Options.ShowDebugInfo;
        }
        _prevF3Down = f3Down;

        ControllerManager.UpdateGlobal();

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

        using (Profiler.Begin("UpdateHud"))
        {
            HUD.Update(1.0f);
        }

        GameRenderer.UpdateTargetedEntity(1.0F);
        GameRenderer.tick(partialTicks);

        using (Profiler.Begin("UpdatePlayerController"))
        {
            if (!IsGamePaused && World != null)
            {
                PlayerController.updateController();
            }
        }

        using (Profiler.Begin("UpdateDynamicTextures"))
        {
            TextureManager.BindTexture(TextureManager.GetTextureId("/terrain.png"));
            if (!IsGamePaused)
            {
                TextureManager.Tick();
            }
        }

        if (CurrentScreen == null && Player != null)
        {
            if (Player.Health <= 0)
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

            using (Profiler.Begin("UpdateEntityRenderer"))
            {
                if (!IsGamePaused)
                {
                    GameRenderer.updateCamera();
                }
            }

            if (!IsGamePaused)
            {
                WorldRenderer.UpdateClouds();
            }

            using (Profiler.Begin("TickEntities"))
            {
                if (!IsGamePaused)
                {
                    if (World.Environment.LightningTicksLeft > 0)
                    {
                        --World.Environment.LightningTicksLeft;
                    }
                    World.Entities.TickEntities();
                }
            }

            using (Profiler.Begin("TickWorld"))
            {
                if (!IsGamePaused || (IsMultiplayerWorld() && InternalServer == null))
                {
                    World.allowSpawning(Options.Difficulty > 0, true);
                    World.Tick();
                }
            }

            if (!IsGamePaused && World != null)
            {
                World.displayTick(MathHelper.Floor(Player.X), MathHelper.Floor(Player.Y), MathHelper.Floor(Player.Z));
            }

            if (!IsGamePaused)
            {
                ParticleManager.updateEffects();
            }
        }

        _systemTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

                    bool zoomHeld = CurrentScreen == null && InGameHasFocus && Keyboard.isKeyDown(Options.KeyBindZoom.scanCode);
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

                        Options.ZoomScale = Math.Clamp(Options.ZoomScale, 1.25F, 20.0F);
                    }
                    else
                    {
                        Player.inventory.ChangeCurrentItem(mouseWheelDelta);
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
            // Block key-down events when ImGui has keyboard focus.
            if (ImGuiInput.CapturingKeyboard)
            {
                continue;
            }

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

                    if (Keyboard.getEventKey() == Keyboard.KEY_F5)
                    {
                        Options.CameraMode = (CameraMode)((int)(Options.CameraMode + 2) % 3);
                    }

                    if (Keyboard.getEventKey() == Keyboard.KEY_F8) Options.SmoothCamera = !Options.SmoothCamera;
                    if (Keyboard.getEventKey() == Keyboard.KEY_F7) ShowChunkBorders = !ShowChunkBorders;

                    if (Keyboard.getEventKey() == Options.KeyBindInventory.scanCode)
                    {
                        Navigate(new InventoryScreen(UIContext, Player, PlayerController, () => CurrentScreen));
                    }

                    if (Keyboard.getEventKey() == Options.KeyBindDrop.scanCode) Player.DropSelectedItem();

                    if (Keyboard.getEventKey() == Options.KeyBindChat.scanCode)
                    {
                        Navigate(new ChatScreen(UIContext, HUD.Chat, Player));
                    }

                    if (Keyboard.getEventKey() == Options.KeyBindCommand.scanCode)
                    {
                        Navigate(new ChatScreen(UIContext, HUD.Chat, Player, "/"));
                    }
                }

                for (int slotIndex = 0; slotIndex < 9; ++slotIndex)
                {
                    if (Keyboard.getEventKey() == Keyboard.KEY_1 + slotIndex)
                    {
                        Player.inventory.SelectedSlot = slotIndex;
                    }
                }

                if (Keyboard.getEventKey() == Options.KeyBindToggleFog.scanCode)
                {
                    Options.RenderDistanceOption.Value = Math.Clamp(
                        Options.RenderDistanceOption.Value + (!Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) && !Keyboard.isKeyDown(Keyboard.KEY_RSHIFT) ? 1.0f / 28.0f : -1.0f / 28.0f),
                        0.0f,
                        1.0f);
                }
            }
        }


        ControllerManager.UpdateUI(CurrentScreen);
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
                    ItemStack selectedItem = Player.inventory.GetItemInHand();
                    int itemCountBefore = selectedItem != null ? selectedItem.Count : 0;
                    if (PlayerController.sendPlaceBlock(Player, World, selectedItem, blockX, blockY, blockZ, blockSide))
                    {
                        shouldPerformSecondaryAction = false;
                        Player.swingHand();
                    }

                    if (selectedItem == null)
                    {
                        return;
                    }

                    if (selectedItem.Count == 0)
                    {
                        Player.inventory.Main[Player.inventory.SelectedSlot] = null;
                    }
                    else if (selectedItem.Count != itemCountBefore)
                    {
                        GameRenderer.itemRenderer.ResetEquippedProgress();
                    }
                }
            }

            if (shouldPerformSecondaryAction && mouseButton == 1)
            {
                ItemStack selectedItem = Player.inventory.GetItemInHand();
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
            int backupId = 0;

            if (blockId == Block.GrassBlock.id) backupId = Block.Dirt.id;
            else if (blockId == Block.Bedrock.id) backupId = Block.Stone.id;
            else if (blockId == Block.Leaves.id) backupId = Block.Sapling.id;
            else if (blockId == Block.DoubleSlab.id) blockId = Block.Slab.id;

            Player.inventory.SetCurrentItem(blockId, backupId);
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
        _loadingScreen.BeginLoading(loadingText);
        _loadingScreen.SetStage("");
        SoundManager.PlayStreaming(null!, 0.0F, 0.0F, 0.0F, 0.0F, 0.0F);

        World = newWorld!;
        if (newWorld != null)
        {
            PlayerController.ChangeWorld(newWorld);
            if (!IsMultiplayerWorld())
            {
                if (targetEntity == null)
                {
                    Player = (ClientPlayerEntity?)World.GetPlayerForProxy(typeof(ClientPlayerEntity));
                }
            }
            else if (Player != null)
            {
                Player.TeleportToTop();
                newWorld?.Entities.SpawnEntity(Player);
            }

            if (Player == null)
            {
                Player = (ClientPlayerEntity)PlayerController.createPlayer(newWorld);
                Player.TeleportToTop();
                PlayerController.flipPlayer(Player);
            }

            Player.movementInput = new MovementInputFromOptions(Options);
            WorldRenderer?.ChangeWorld(newWorld);
            ParticleManager?.clearEffects(newWorld);

            PlayerController.fillHotbar(Player);
            if (targetEntity != null)
            {
                World.SaveWorldData();
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
            previousPlayerId = Player.ID;
            World.Entities.Remove(Player);
        }

        Player = (ClientPlayerEntity)PlayerController.createPlayer(World);
        Player.dimensionId = newDimensionId;
        Player.TeleportToTop();

        if (useBedSpawn)
        {
            Player.setSpawnPos(playerSpawnPos);
            Player.SetPositionAndAnglesKeepPrevAngles(
                finalRespawnPos.X + 0.5,
                finalRespawnPos.Y + 0.1,
                finalRespawnPos.Z + 0.5,
                0.0F,
                0.0F);
        }

        PlayerController.flipPlayer(Player);
        World.AddPlayer(Player);
        Player.movementInput = new MovementInputFromOptions(Options);
        Player.ID = previousPlayerId;
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
        InternalServer.RegistryAccess = RegistryAccess;
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
        _loadingScreen.BeginLoading(loadingText);
        _loadingScreen.SetStage("Building terrain");
        short loadingRadius = 128;
        int loadedChunkCount = 0;
        int totalChunksToLoad = loadingRadius * 2 / 16 + 1;
        totalChunksToLoad *= totalChunksToLoad;
        Vec3i centerPos = World.Properties.GetSpawnPos();

        if (Player != null)
        {
            centerPos.X = (int)Player.X;
            centerPos.Z = (int)Player.Z;
        }

        for (int xOffset = -loadingRadius; xOffset <= loadingRadius; xOffset += 16)
        {
            for (int zOffset = -loadingRadius; zOffset <= loadingRadius; zOffset += 16)
            {
                _loadingScreen.SetProgress(loadedChunkCount++ * 100 / totalChunksToLoad);
                World.Reader.GetBlockId(centerPos.X + xOffset, 64, centerPos.Z + zOffset);

                while (World.Lighting.DoLightingUpdates())
                {
                }
            }
        }

        _loadingScreen.SetStage("Simulating world for a bit");
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
        else if (newScreen == null && Player.Health <= 0)
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
            Vector2D<int> inputSizeForReset = UIContext.InputDisplaySize;
            VirtualCursor.Reset(inputSizeForReset.X, inputSizeForReset.Y);
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
            Navigate(new IngameMenuScreen(UIContext, StatFileWriter, SetIngameFocus, quitText, () =>
            {
                if (IsMultiplayerWorld()) World.Disconnect();
                StopInternalServer();
                ChangeWorld(null);
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
                MouseHelper.GrabMouseCursor();
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
            MouseHelper.UngrabMouseCursor();
            Mouse.setCursorVisible(!IsControllerMode);
        }
    }

    private MainMenuScreen CreateMainMenuScreen() => new(UIContext, Session, _hideQuitButton, this, CreateNetworkContext(), TexturePackList, Shutdown);
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

        FramebufferManager.Resize(Display.getFramebufferWidth(), Display.getFramebufferHeight());
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
        ScaledResolution scaledResolution = new(Options, DisplayWidth, DisplayHeight);
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0D, scaledResolution.ScaledWidth, scaledResolution.ScaledHeight, 0.0D, 1000.0D, 3000.0D);
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
        short logoWidth = 256;
        short logoHeight = 256;
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        tessellator.setColorOpaque_I(0xFFFFFF);
        DrawTextureRegion((scaledResolution.ScaledWidth - logoWidth) / 2, (scaledResolution.ScaledHeight - logoHeight) / 2, 0, 0, logoWidth, logoHeight);
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

        PlayerNameValidator.Validate(result.Name);

        StartMainThread(result.Name, result.Session);
    }

    private static void StartMainThread(string? playerName, string? sessionToken)
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
