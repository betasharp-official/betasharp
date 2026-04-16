using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using BetaSharp.Blocks;
using BetaSharp.Client.Achievements;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Input;
using BetaSharp.Client.Network;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Backends;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Client.Rendering.Presentation;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Blocks.Entities;
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
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

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
    public RendererBackendKind RequestedRendererBackend { get; }
    public RendererBackendKind ActiveRendererBackend { get; private set; } = RendererBackendKind.OpenGL;
    public string? RendererFallbackReason { get; private set; }
    public RendererBackendKind ImGuiRendererBackend { get; private set; } = RendererBackendKind.OpenGL;
    public RendererBackendKind PresentationRendererBackend { get; private set; } = RendererBackendKind.OpenGL;
    public bool IsRendererRuntimeInitialized => _isRenderBackendInitialized;
    public RendererBackendCapabilities ActiveRendererCapabilities =>
        _isRenderBackendInitialized
            ? _renderBackendBootstrap.Capabilities
            : RendererBackendCapabilities.For(ActiveRendererBackend);
    public RendererBackendStateSnapshot RendererBackendState => new(
        RequestedBackend: RequestedRendererBackend,
        ActiveBackend: ActiveRendererBackend,
        DisplayBackend: Display.ActiveRendererBackend,
        ImGuiBackend: ImGuiRendererBackend,
        PresentationBackend: PresentationRendererBackend,
        RuntimeCapabilities: ActiveRendererCapabilities,
        IsRuntimeInitialized: IsRendererRuntimeInitialized,
        DisplaySupportsWindowBufferSwap: Display.SupportsWindowBufferSwap,
        DisplayHasOpenGlContext: Display.HasOpenGlContext,
        PresentationTargetWidth: PresentationTargetWidth,
        PresentationTargetHeight: PresentationTargetHeight,
        IsPresentationBlitSkipped: IsPresentationBlitSkipped,
        FallbackReason: RendererFallbackReason);
    public bool SupportsLegacyOpenGlRenderPath => ActiveRendererCapabilities.SupportsLegacyOpenGlRenderPath;
    public bool SupportsScreenshotCapture => ActiveRendererCapabilities.SupportsScreenshotCapture;

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

    public ISceneOrchestrator SceneOrchestrator { get; private set; } = new NoOpSceneOrchestrator();
    public IWorldRenderer WorldRenderer { get; private set; } = new NoOpWorldRenderer();
    public ILegacyFixedFunctionApi LegacyFixedFunctionApi { get; private set; } = new NoOpLegacyFixedFunctionApi();

    public FrameContext FrameContext { get; private set; } = new(new NoOpLegacyFixedFunctionApi());
    public int PresentationTargetWidth => _framePresenter.FramebufferWidth;
    public int PresentationTargetHeight => _framePresenter.FramebufferHeight;
    public bool IsPresentationBlitSkipped => _framePresenter.SkipBlit;
    public PresentationViewportImage CurrentPresentationViewportImage => _framePresenter.ViewportImage;
    public ITextureManager TextureManager { get; private set; }
    public ISkinManager SkinManager { get; private set; }
    public ITextRenderer TextRenderer { get; private set; }
    public IEntityRenderDispatcher EntityRenderDispatcher { get; private set; } = new NoOpEntityRenderDispatcher();
    public IBlockEntityRenderDispatcher BlockEntityRenderDispatcher { get; private set; } = new NoOpBlockEntityRenderDispatcher();
    public TexturePacks TexturePackList { get; private set; }
    public IParticleManager ParticleManager { get; private set; } = new NoOpParticleManager();

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
    private ILoadingScreenRenderer _loadingScreen;
    private readonly DebugTelemetry _debugTelemetry = new();

    private readonly string _serverName;
    private readonly int _serverPort;
    private readonly bool _hideQuitButton;


    private DebugWindowManager _debugWindowManager;
    private IImGuiRendererBackend _imguiRendererBackend = null!;
    private IRenderBackendBootstrap _renderBackendBootstrap = null!;
    private IRendererFactory _rendererFactory = null!;
    private IRendererServices _rendererServices = null!;
    private IFramePresenter _framePresenter = new NoOpFramePresenter(RendererBackendKind.OpenGL);
    private bool _isRenderBackendInitialized;
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

    public BetaSharp(int width, int height, bool isFullscreen, RendererBackendKind rendererBackend = RendererBackendKind.OpenGL)
    {
        _loadingScreen = new NoOpLoadingScreenRenderer(this);
        _tempDisplayHeight = height;
        _fullscreen = isFullscreen;
        DisplayWidth = width;
        DisplayHeight = height;
        RequestedRendererBackend = rendererBackend;
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

        SetupInputAndRendering();
        SetupResourcesAndPostProcessing();

        CheckRenderBackendErrors("Post startup");

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
        Options.ReloadChunks += () => { SceneOrchestrator.MarkVisibleChunksDirty(); };

        Profiler.RegisterMainThread();

        try
        {
            int[] msaaValues = [0, 2, 4, 8];
            Display.MSAA_Samples = msaaValues[Options.MSAALevel];

            RendererBackendSelection backendSelection = RendererBackendFactory.Resolve(RequestedRendererBackend);
            ActiveRendererBackend = backendSelection.Effective;
            RendererFallbackReason = backendSelection.FallbackReason;
            _renderBackendBootstrap = RenderBackendBootstrapFactory.Create(ActiveRendererBackend);
            _rendererFactory = _renderBackendBootstrap.RendererFactory;
            _loadingScreen = _rendererFactory.CreateLoadingScreenRenderer(this);
            _isRenderBackendInitialized = false;

            _logger.LogInformation(
                "Renderer backend requested: {RequestedBackend}; active: {ActiveBackend}",
                backendSelection.Requested,
                backendSelection.Effective);

            if (!string.IsNullOrWhiteSpace(backendSelection.FallbackReason))
            {
                _logger.LogWarning("{FallbackReason}", backendSelection.FallbackReason);
            }

            Display.create(ActiveRendererBackend);
            Display.getGlfw().SetWindowSizeLimits(Display.GetWindowHandle(), 850, 480, maximumWidth, maximumHeight);

            _renderBackendBootstrap.InitializeGraphicsContext(_debugTelemetry);
            _renderBackendBootstrap.ConfigurePresentationMode(Options);
            _isRenderBackendInitialized = true;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception initializing display");
            _isRenderBackendInitialized = false;
            throw new InvalidOperationException($"Failed to initialize renderer backend '{ActiveRendererBackend}'.", ex);
        }
    }

    private void SetupCoreSystems()
    {
        TexturePackList = new TexturePacks(this, new DirectoryInfo(_gameDataDir));
        _rendererServices = _rendererFactory.CreateServices(this, TexturePackList, Options);
        TextureManager = _rendererServices.TextureManager;
        TextRenderer = _rendererServices.TextRenderer;
        SkinManager = _rendererServices.SkinManager;
        EntityRenderDispatcher = _rendererServices.EntityRenderDispatcher;
        BlockEntityRenderDispatcher = _rendererServices.BlockEntityRenderDispatcher;
        LegacyFixedFunctionApi = _rendererServices.LegacyFixedFunctionApi;
        FrameContext = new FrameContext(LegacyFixedFunctionApi);

        UIContext = new UIContext(
            Options,
            TextRenderer,
            TextureManager,
            EntityRenderDispatcher,
            BlockEntityRenderDispatcher,
            _rendererServices.UiRenderBackend,
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

        WaterColors.loadColors(TextureManager.GetColors("/misc/watercolor.png"));
        GrassColors.loadColors(TextureManager.GetColors("/misc/grasscolor.png"));
        FoliageColors.loadColors(TextureManager.GetColors("/misc/foliagecolor.png"));
        SceneOrchestrator = _rendererFactory.CreateSceneOrchestrator(this);
        _rendererServices.ConfigureEntityRendering(this);
        StatFileWriter = new StatFileWriter(Session, _gameDataDir);

        StatStringFormatKeyInv format = new(this);
        global::BetaSharp.Achievements.OpenInventory.GetTranslatedDescription = () =>
        {
            return format.formatString(global::BetaSharp.Achievements.OpenInventory.TranslationKey);
        };
    }

    private unsafe void SetupInputAndRendering()
    {
        ImGui.CreateContext();

        ImGuiIO* io = ImGui.GetIO();
        io->ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;

        // Install game input callbacks first so the ImGui GLFW backend can chain to them.
        Keyboard.create(Display.getGlfw(), Display.GetWindowHandle());
        Mouse.create(Display.getGlfw(), Display.GetWindowHandle(), Display.getWidth(), Display.getHeight());
        Controller.Create(Display.getGlfw(), Display.GetWindowHandle());

        _imguiRendererBackend = _rendererFactory.CreateImGuiRendererBackend();
        ImGuiRendererBackend = _imguiRendererBackend.BackendKind;
        _imguiRendererBackend.Initialize((nint)Display.GetWindowHandle());
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

        CheckRenderBackendErrors("Pre startup");
        _renderBackendBootstrap.ConfigureDefaultRenderState(Options, _logger);
        CheckRenderBackendErrors("Startup");
    }

    private void SetupResourcesAndPostProcessing()
    {
        RegistryAccess = RegistryAccess.Build();

        SoundManager.LoadSoundSettings(Options);
        DefaultMusicCategories.Register(SoundManager);

        _rendererServices.RegisterDynamicTextures(this);

        WorldRenderer = _rendererFactory.CreateWorldRenderer(this, TextureManager);
        SetMainViewport(Display.getFramebufferWidth(), Display.getFramebufferHeight());
        ParticleManager = _rendererFactory.CreateParticleManager(World, TextureManager);

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

        _framePresenter = _rendererFactory.CreateFramePresenter(
             Display.getFramebufferWidth(),
             Display.getFramebufferHeight(),
             Options);
        PresentationRendererBackend = _framePresenter.BackendKind;
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
            try { _renderBackendBootstrap.CleanupRenderResources(); } catch (Exception) { }

            // don't bother trying to shutdown imgui because it keeps hanging/crashing

            SkinManager.Dispose();
            TextureManager.Dispose();
            SoundManager.Dispose();
            Mouse.destroy();
            Keyboard.destroy();

            _renderBackendBootstrap.LogRenderResourceReport();
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
                        _imguiRendererBackend.NewFrame();
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
                    CheckRenderBackendErrors("Pre render");

                    SoundManager.UpdateListener(Player, Timer.renderPartialTicks);
                    PrepareFrameRenderState();

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
                            UpdateWindow(true);
                        }
                    }

                    if (Player != null && Player.isInsideWall())
                    {
                        Options.CameraMode = EnumCameraMode.FirstPerson;
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
                                ResizePresentationTarget(vpW, vpH);
                                _lastViewportSize = vpSize;
                            }
                            DisplayWidth = vpW;
                            DisplayHeight = vpH;

                            DebugViewportOffset = new Vector2(
                                _debugWindowManager.ViewportPos.X,
                                Display.getHeight() - vpH - _debugWindowManager.ViewportPos.Y);
                            SetPresentationBlitSkipped(true);
                        }
                        else
                        {
                            DebugViewportOffset = Vector2.Zero;
                            SetPresentationBlitSkipped(false);
                        }
                    }
                    else
                    {
                        DebugViewportOffset = Vector2.Zero;
                        SetPresentationBlitSkipped(false);
                        if (_lastViewportSize != Vector2.Zero)
                        {
                            ResizePresentationToWindowFramebuffer();
                            _lastViewportSize = Vector2.Zero;
                            _debugWindowManager.ViewportImage = PresentationViewportImage.Empty;
                        }
                    }

                    if (!SkipRenderWorld)
                    {
                        PlayerController?.setPartialTime(Timer.renderPartialTicks);

                        TextureStats.StartFrame();

                        using (Profiler.Begin("Render"))
                        {
                            SceneOrchestrator.OnFrameUpdate(Timer.renderPartialTicks);
                        }

                        TextureStats.EndFrame();
                        SceneOrchestrator.PublishRenderMetrics();
                    }

                    DisplayWidth = savedWidth;
                    DisplayHeight = savedHeight;

                    if (imguiThisFrame)
                    {
                        if (IsPresentationBlitSkipped)
                        {
                            _debugWindowManager.ViewportImage = CurrentPresentationViewportImage;
                        }

                        using (Profiler.Begin("ImguiBuild"))
                        {
                            _debugWindowManager.Render(Timer.DeltaTime);
                        }

                        using (Profiler.Begin("ImguiSubmit"))
                        {
                            ImGui.Render();
                            _imguiRendererBackend.RenderDrawData();
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
                        UpdateWindow(true);
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

                    CheckRenderBackendErrors("Post render");
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

        SceneOrchestrator.UpdateTargetedEntity(1.0F);
        SceneOrchestrator.Tick(partialTicks);

        using (Profiler.Begin("UpdatePlayerController"))
        {
            if (!IsGamePaused && World != null)
            {
                PlayerController.updateController();
            }
        }

        using (Profiler.Begin("UpdateDynamicTextures"))
        {
            _renderBackendBootstrap.UpdateDynamicTextures(TextureManager, IsGamePaused);
        }

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

            using (Profiler.Begin("UpdateEntityRenderer"))
            {
                if (!IsGamePaused)
                {
                    SceneOrchestrator.UpdateCamera();
                }
            }

            if (!IsGamePaused)
            {
                SceneOrchestrator.UpdateClouds();
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
                World.displayTick(MathHelper.Floor(Player.x), MathHelper.Floor(Player.y), MathHelper.Floor(Player.z));
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
                        Player.inventory.SelectedSlot = slotIndex;
                    }
                }

                if (Keyboard.getEventKey() == Options.KeyBindToggleFog.keyCode)
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
                        SceneOrchestrator.ResetEquippedItemProgress();
                    }
                }
            }

            if (shouldPerformSecondaryAction && mouseButton == 1)
            {
                ItemStack selectedItem = Player.inventory.GetItemInHand();
                if (selectedItem != null && PlayerController.sendUseItem(Player, World, selectedItem))
                {
                    SceneOrchestrator.ResetEquippedItemProgress();
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
            SceneOrchestrator.ChangeWorld(newWorld);
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
            centerPos.X = (int)Player.x;
            centerPos.Z = (int)Player.z;
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
    private ClientNetworkContext CreateNetworkContext() => new(this, this, this, Session, StatFileWriter, ParticleManager, EntityRenderDispatcher, HUD.AddChatMessage, this);

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
            UpdateWindow(true);
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

        ResizePresentationToWindowFramebuffer();
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

                if (TryCaptureScreenshot(framebufferWidth, framebufferHeight, out byte[] pixels))
                {
                    string result = ScreenShotHelper.saveScreenshot(_gameDataDir, DisplayWidth, DisplayHeight, pixels);
                    HUD.AddChatMessage(result);
                }
                else
                {
                    HUD.AddChatMessage($"Screenshots are not supported yet for renderer '{ActiveRendererBackend}'.");
                }
            }
        }
        else
        {
            _isTakingScreenshot = false;
        }
    }

    private bool TryCaptureScreenshot(int framebufferWidth, int framebufferHeight, out byte[] pixels)
    {
        pixels = [];

        if (!SupportsScreenshotCapture)
        {
            return false;
        }

        return _renderBackendBootstrap.TryCaptureScreenshot(framebufferWidth, framebufferHeight, out pixels);
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
    private void CheckRenderBackendErrors(string location)
    {
        if (!_isRenderBackendInitialized)
        {
            return;
        }

        _renderBackendBootstrap.CheckBackendErrors(location, _logger);
    }

    private void LoadScreen()
    {
        TextureHandle splash = TextureManager.GetTextureId("/title/mojang.png");

        _renderBackendBootstrap.RenderStartupScreen(
            Options,
            DisplayWidth,
            DisplayHeight,
            Display.getFramebufferWidth(),
            Display.getFramebufferHeight(),
            splash.Id);

        UpdateWindow(true);
    }

    private void SetMainViewport(int width, int height)
    {
        if (!_isRenderBackendInitialized)
        {
            return;
        }

        _renderBackendBootstrap.SetMainViewport(width, height);
    }

    private void PrepareFrameRenderState()
    {
        if (!_isRenderBackendInitialized)
        {
            return;
        }

        _renderBackendBootstrap.PrepareFrameRenderState();
    }

    public void BeginPresentationFrame()
    {
        _framePresenter.Begin();
    }

    public void EndPresentationFrame()
    {
        _framePresenter.End();
    }

    public void ResizePresentationTarget(int width, int height)
    {
        _framePresenter.Resize(width, height);
    }

    public void ResizePresentationToWindowFramebuffer()
    {
        _framePresenter.Resize(Display.getFramebufferWidth(), Display.getFramebufferHeight());
    }

    public void SetPresentationBlitSkipped(bool skipped)
    {
        _framePresenter.SkipBlit = skipped;
    }

    public void UpdateWindow(bool processMessages = true)
    {
        if (!_isRenderBackendInitialized)
        {
            Display.update(processMessages);
            return;
        }

        _renderBackendBootstrap.UpdateWindow(processMessages);
    }

    public void SetVSyncEnabled(bool enabled)
    {
        if (!_isRenderBackendInitialized)
        {
            Display.setVSyncEnabled(enabled);
            return;
        }

        _renderBackendBootstrap.SetVSyncEnabled(enabled);
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
        ClientStartupOptions options = ClientStartupArgumentParser.Parse(args);

        PlayerNameValidator.Validate(options.PlayerName);

        StartMainThread(options.PlayerName, options.SessionToken, options.RendererBackend);
    }

    private static void StartMainThread(string? playerName, string? sessionToken, RendererBackendKind rendererBackend)
    {
        Thread.CurrentThread.Name = "BetaSharp Main Thread";

        BetaSharp game = new(850, 480, false, rendererBackend);

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
