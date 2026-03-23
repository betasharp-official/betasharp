using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using BetaSharp.Blocks;
using BetaSharp.Client.Achievements;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.DynamicTexture;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Guis.Debug.Components;
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
using BetaSharp.Client.Guis.Debug;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client;

public partial class BetaSharp
{
    public static BetaSharp Instance = null!;
    private readonly ILogger<BetaSharp> _logger = Log.Instance.For<BetaSharp>();
    public PlayerController playerController;
    private bool fullscreen;
    private bool hasCrashed;
    public int displayWidth;
    public int displayHeight;

    public Timer Timer { get; } = new(20.0F);
    public World world;
    public WorldRenderer terrainRenderer;
    public ClientPlayerEntity player;
    public EntityLiving camera;
    public ParticleManager particleManager;
    public Session session;
    public bool hideQuitButton = false;
    public volatile bool isGamePaused;
    public TextureManager textureManager;
    public SkinManager skinManager;
    public TextRenderer fontRenderer;
    public GuiScreen currentScreen;
    public LoadingScreenRenderer loadingScreen;
    public GameRenderer gameRenderer;
    public PostProcessManager PostProcessManager { get; private set; }
    public int TicksRan { get; private set; }
    private int leftClickCounter;
    private int tempDisplayWidth;
    private int tempDisplayHeight;
    public GuiAchievement guiAchievement;
    public GuiIngame ingameGUI;
    public bool skipRenderWorld;
    public HitResult objectMouseOver = new HitResult(HitResultType.MISS);
    public GameOptions options;
    public DebugComponentsStorage componentsStorage;
    public bool ShowChunkBorders = false;
    public SoundManager sndManager = new();
    public MouseHelper mouseHelper;
    public TexturePacks texturePackList;
    private string gameDataDir;
    private IWorldStorageSource saveLoader;
    public static long[] frameTimes = new long[512];
    public static long[] tickTimes = new long[512];
    public static int numRecordedFrameTimes;
    public static long hasPaidCheckTime = 0L;
    public StatFileWriter statFileWriter;
    private string serverName;
    private int serverPort;
    private readonly WaterSprite textureWaterFX = new();
    private readonly LavaSprite textureLavaFX = new();
    public volatile bool running = true;
    public string debug = "";
    bool isTakingScreenshot;
    long prevFrameTime = -1L;
    public bool inGameHasFocus;
    public int MouseTicksRan { get; set; }
    long systemTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
    private int joinPlayerCounter;
    private ImGuiController imGuiController;
    public InternalServer? internalServer;
    private GLErrorHandler _glErrorHandler;
    private readonly DebugTelemetry _debugTelemetry = new();

    private bool _wasLeftBumperDown;
    private bool _wasRightBumperDown;
    private bool _wasLeftTriggerDown;
    private bool _wasRightTriggerDown;
    private bool _wasStartButtonDown;
    private bool _wasYButtonDown;
    private bool _wasDpadLeftDown;
    private bool _wasDpadRightDown;
    private bool _wasDpadUpDown;
    private bool _wasDpadDownDown;

    public bool isControllerMode;
    public float virtualCursorX;
    public float virtualCursorY;

    public BetaSharp(int width, int height, bool isFullscreen)
    {
        loadingScreen = new LoadingScreenRenderer(this);
        guiAchievement = new GuiAchievement(this);
        tempDisplayHeight = height;
        fullscreen = isFullscreen;
        displayWidth = width;
        displayHeight = height;

        Instance = this;
    }

    [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static partial uint TimeBeginPeriod(uint period);

    [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static partial uint TimeEndPeriod(uint period);

    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);


    public void InitializeTimer()
    {
        if (IsWindows)
        {
            TimeBeginPeriod(1);
        }
    }

    public void CleanupTimer()
    {
        if (IsWindows)
        {
            TimeEndPeriod(1);
        }
    }

    public void onBetaSharpCrash(Exception crashInfo)
    {
        hasCrashed = true;
        _logger.LogError(crashInfo, "BetaSharp has crashed!");
    }

    public unsafe void startGame()
    {
        Bootstrap.Initialize();
        DebugComponents.RegisterComponents();

        InitializeTimer();

        int maximumWidth = Display.getDisplayMode().getWidth();
        int maximumHeight = Display.getDisplayMode().getHeight();

        if (fullscreen)
        {
            Display.setFullscreen(true);

            displayWidth = maximumWidth;
            displayHeight = maximumHeight;

            if (displayWidth <= 0)
            {
                displayWidth = 1;
            }

            if (displayHeight <= 0)
            {
                displayHeight = 1;
            }
        }
        else
        {
            Display.setDisplayMode(new DisplayMode(displayWidth, displayHeight));
            Display.setLocation((maximumWidth - displayWidth) / 2, (maximumHeight - displayHeight) / 2);
        }

        Display.setTitle("BetaSharp Beta 1.7.3");

        gameDataDir = getBetaSharpDir();
        saveLoader = new RegionWorldStorageSource(Path.Combine(gameDataDir, "saves"));
        options = new GameOptions(this, gameDataDir);
        componentsStorage = new DebugComponentsStorage(this, gameDataDir);
        Profiler.Enabled = options.DebugMode;
        Profiler.EnableLagSpikeDetection = options.DebugMode;
        Profiler.LagSpikeDirectory = Path.Combine(gameDataDir, "logs", "lag_spikes");

        try
        {
            int[] msaaValues = [0, 2, 4, 8];
            Display.MSAA_Samples = msaaValues[options.MSAALevel];
            Display.DebugMode = options.DebugMode;

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

            Display.getGlfw().SwapInterval(options.VSync ? 1 : 0);

            if (options.DebugMode)
            {
                _glErrorHandler = new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception");
        }
        texturePackList = new TexturePacks(this, new DirectoryInfo(gameDataDir));
        textureManager = new TextureManager(this, texturePackList, options);
        fontRenderer = new TextRenderer(options, textureManager);
        skinManager = new SkinManager(textureManager);
        WaterColors.loadColors(textureManager.GetColors("/misc/watercolor.png"));
        GrassColors.loadColors(textureManager.GetColors("/misc/grasscolor.png"));
        FoliageColors.loadColors(textureManager.GetColors("/misc/foliagecolor.png"));
        gameRenderer = new GameRenderer(this);
        EntityRenderDispatcher.instance.skinManager = skinManager;
        EntityRenderDispatcher.instance.heldItemRenderer = new HeldItemRenderer(this);
        statFileWriter = new StatFileWriter(session, gameDataDir);

        StatStringFormatKeyInv format = new(this);
        global::BetaSharp.Achievements.OpenInventory.GetTranslatedDescription = () =>
        {
            return format.formatString(global::BetaSharp.Achievements.OpenInventory.TranslationKey);
        };

        loadScreen();

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
            imGuiController = new(((LegacyGL)GLManager.GL).SilkGL, window, input);
            imGuiController.MakeCurrent();
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to initialize ImGui: {e}");
            imGuiController = null;
        }

        Keyboard.create(Display.getGlfw(), Display.getWindowHandle());
        Mouse.create(Display.getGlfw(), Display.getWindowHandle(), Display.getWidth(), Display.getHeight());
        Controller.Create(Display.getGlfw(), Display.getWindowHandle());
        ControllerManager.Initialize(this);
        mouseHelper = new MouseHelper();

        checkGLError("Pre startup");
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
        checkGLError("Startup");
        sndManager.LoadSoundSettings(options);
        DefaultMusicCategories.Register(sndManager);
        textureManager.AddDynamicTexture(textureLavaFX);
        textureManager.AddDynamicTexture(textureWaterFX);
        textureManager.AddDynamicTexture(new NetherPortalSprite());
        textureManager.AddDynamicTexture(new CompassSprite(this));
        textureManager.AddDynamicTexture(new ClockSprite(this));
        textureManager.AddDynamicTexture(new WaterSideSprite());
        textureManager.AddDynamicTexture(new LavaSideSprite());
        textureManager.AddDynamicTexture(new FireSprite(0));
        textureManager.AddDynamicTexture(new FireSprite(1));
        terrainRenderer = new WorldRenderer(this, textureManager);
        GLManager.GL.Viewport(0, 0, (uint)Display.getFramebufferWidth(), (uint)Display.getFramebufferHeight());
        particleManager = new ParticleManager(world, textureManager);

        string dataDirPath = gameDataDir;

        _ = new ResourceManager()
            .Add(new BetaResourceDownloader(this, dataDirPath))
            .Add(new ModernAssetDownloader(this, dataDirPath,
                [
                 "minecraft/sounds/music/menu/moog_city_2.ogg",
                 "minecraft/sounds/music/menu/mutation.ogg",
                 "minecraft/sounds/music/menu/floating_trees.ogg",
                 "minecraft/sounds/music/menu/beginning_2.ogg",
                ])).LoadAllAsync();

        checkGLError("Post startup");
        ingameGUI = new GuiIngame(this);
        PostProcessManager = new PostProcessManager(Display.getFramebufferWidth(), Display.getFramebufferHeight(), options);

        statFileWriter.ReadStat(Stats.Stats.StartGameStat, 1);
        if (serverName != null)
        {
            displayGuiScreen(new GuiConnecting(this, serverName, serverPort));
        }
        else
        {
            displayGuiScreen(new GuiMainMenu());
        }
    }

    private void loadScreen()
    {
        ScaledResolution var1 = new(options, displayWidth, displayHeight);
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0D, var1.ScaledWidthDouble, var1.ScaledHeightDouble, 0.0D, 1000.0D, 3000.0D);
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
        textureManager.BindTexture(textureManager.GetTextureId("/title/mojang.png"));
        tessellator.startDrawingQuads();
        tessellator.setColorOpaque_I(0xFFFFFF);
        tessellator.addVertexWithUV(0.0D, (double)displayHeight, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV((double)displayWidth, (double)displayHeight, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV((double)displayWidth, 0.0D, 0.0D, 0.0D, 0.0D);
        tessellator.addVertexWithUV(0.0D, 0.0D, 0.0D, 0.0D, 0.0D);
        tessellator.draw();
        short var3 = 256;
        short var4 = 256;
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        tessellator.setColorOpaque_I(0xFFFFFF);
        drawTextureRegion((var1.ScaledWidth - var3) / 2, (var1.ScaledHeight - var4) / 2, 0, 0, var3, var4);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.Fog);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.AlphaFunc(GLEnum.Greater, 0.1F);
        Display.swapBuffers();
    }

    public void drawTextureRegion(int x, int y, int texX, int texY, int width, int height)
    {
        float uScale = 1 / 256f;
        float vScale = 1 / 256f;

        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(x + 0, y + height, 0, (texX + 0) * uScale, (texY + height) * vScale);
        tess.addVertexWithUV(x + width, y + height, 0, (texX + width) * uScale, (texY + height) * vScale);
        tess.addVertexWithUV(x + width, y + 0, 0, (texX + width) * uScale, (texY + 0) * vScale);
        tess.addVertexWithUV(x + 0, y + 0, 0, (texX + 0) * uScale, (texY + 0) * vScale);
        tess.draw();
    }

    public static string getBetaSharpDir()
    {
        return PathHelper.GetAppDir(nameof(BetaSharp));
    }

    public IWorldStorageSource getSaveLoader()
    {
        return saveLoader;
    }

    public void displayGuiScreen(GuiScreen? newScreen)
    {
        Mouse.ClearEvents();
        Controller.ClearEvents();
        currentScreen?.OnGuiClosed();

        if (newScreen is GuiMainMenu)
        {
            statFileWriter.Tick();

            if (inGameHasFocus)
            {
                sndManager.StopCurrentMusic();
            }
        }

        statFileWriter.SyncStats();
        if (newScreen == null && world == null)
        {
            newScreen = new GuiMainMenu();
        }
        else if (newScreen == null && player.health <= 0)
        {
            newScreen = new GuiGameOver();
        }

        if (newScreen is GuiMainMenu)
        {
            ingameGUI.ClearChatMessages();
        }

        currentScreen = newScreen;

        if (currentScreen != null)
        {
            virtualCursorX = displayWidth / 2.0f;
            virtualCursorY = displayHeight / 2.0f;
        }

        if (internalServer != null)
        {
            bool shouldPause = newScreen?.PausesGame ?? false;
            internalServer.SetPaused(shouldPause);
        }

        if (newScreen != null)
        {
            setIngameNotInFocus();
            ScaledResolution scaledResolution = new(options, displayWidth, displayHeight);
            int scaledWidth = scaledResolution.ScaledWidth;
            int scaledHeight = scaledResolution.ScaledHeight;
            newScreen.SetWorldAndResolution(this, scaledWidth, scaledHeight);
            skipRenderWorld = false;
        }
        else
        {
            setIngameFocus();
            sndManager.StopMusic(DefaultMusicCategories.Menu);
        }
    }

    [Conditional("DEBUG")]
    private void checkGLError(string location)
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

    public void ShutdownBetaSharpApplet()
    {
        try
        {
            stopInternalServer();
            statFileWriter.Tick();
            statFileWriter.SyncStats();

            _logger.LogInformation("Stopping!");

            try
            {
                changeWorld((World)null);
            }
            catch (Exception) { }

            try
            {
                GLAllocation.deleteTexturesAndDisplayLists();
            }
            catch (Exception) { }

            skinManager.Dispose();
            textureManager.Dispose();
            sndManager.CloseBetaSharp();
            Mouse.destroy();
            Keyboard.destroy();

            GLTexture.LogLeakReport();
        }
        finally
        {
            Display.destroy();
            CleanupTimer();

            if (!hasCrashed)
            {
                Environment.Exit(0);
            }
        }
    }

    public void Run()
    {
        running = true;

        try
        {
            startGame();
        }
        catch (Exception startupException)
        {
            onBetaSharpCrash(startupException);
            return;
        }

        try
        {
            long lastFpsCheckTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int frameCounter = 0;

            while (running)
            {
                long frameStartNano = Stopwatch.GetTimestamp();

                int startGcGen0 = GC.CollectionCount(0);
                int startGcGen1 = GC.CollectionCount(1);
                int startGcGen2 = GC.CollectionCount(2);

                if (options.DebugMode)
                {
                    Profiler.Update(Timer.DeltaTime);
                    Profiler.PushGroup("run");
                }
                try
                {
                    if (Display.isCloseRequested())
                    {
                        shutdown();
                    }

                    Controller.PollEvents();
                    if (Controller.IsActive())
                    {
                        if (!isControllerMode)
                        {
                            Mouse.setCursorVisible(false);
                            isControllerMode = true;
                        }
                    }

                    if (isControllerMode && currentScreen != null)
                    {
                        float lx = Controller.LeftStickX;
                        float ly = Controller.LeftStickY;

                        bool dpadLeft = Controller.IsButtonDown(Silk.NET.GLFW.GamepadButton.DPadLeft);
                        bool dpadRight = Controller.IsButtonDown(Silk.NET.GLFW.GamepadButton.DPadRight);
                        bool dpadUp = Controller.IsButtonDown(Silk.NET.GLFW.GamepadButton.DPadUp);
                        bool dpadDown = Controller.IsButtonDown(Silk.NET.GLFW.GamepadButton.DPadDown);

                        bool dpadHandled = false;

                        if (currentScreen != null)
                        {
                            int dpadX = 0, dpadY = 0;
                            if (dpadLeft && !_wasDpadLeftDown) dpadX = -1;
                            if (dpadRight && !_wasDpadRightDown) dpadX = 1;
                            if (dpadUp && !_wasDpadUpDown) dpadY = -1;
                            if (dpadDown && !_wasDpadDownDown) dpadY = 1;

                            if (dpadX != 0 || dpadY != 0)
                            {
                                dpadHandled = currentScreen.HandleDPadNavigation(dpadX, dpadY, ref virtualCursorX, ref virtualCursorY);
                            }
                        }

                        _wasDpadLeftDown = dpadLeft;
                        _wasDpadRightDown = dpadRight;
                        _wasDpadUpDown = dpadUp;
                        _wasDpadDownDown = dpadDown;

                        if (!dpadHandled)
                        {
                            if (dpadLeft) lx = -0.2f;
                            if (dpadRight) lx = 0.2f;
                            if (dpadUp) ly = -0.2f;
                            if (dpadDown) ly = 0.2f;
                        }

                        ScaledResolution sr = new(options, displayWidth, displayHeight);
                        float speed = 200f * sr.ScaleFactor;

                        virtualCursorX += lx * speed * Timer.DeltaTime;
                        virtualCursorY += ly * speed * Timer.DeltaTime;

                        if (virtualCursorX < 0) virtualCursorX = 0;
                        if (virtualCursorX > displayWidth) virtualCursorX = displayWidth;
                        if (virtualCursorY < 0) virtualCursorY = 0;
                        if (virtualCursorY > displayHeight) virtualCursorY = displayHeight;
                    }

                    if (isGamePaused && world != null)
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
                    if (options.DebugMode)
                    {
                        Profiler.PushGroup("runTicks");
                    }

                    for (int tickIndex = 0; tickIndex < Timer.elapsedTicks; ++tickIndex)
                    {
                        ++TicksRan;

                        runTick(Timer.renderPartialTicks);
                    }

                    if (options.DebugMode)
                    {
                        Profiler.PopGroup();
                    }

                    long tickElapsedTime = Stopwatch.GetTimestamp() - tickStartTime;
                    checkGLError("Pre render");
                    sndManager.UpdateListener(player, Timer.renderPartialTicks);
                    GLManager.GL.Enable(GLEnum.Texture2D);
                    if (world != null)
                    {
                        if (options.DebugMode) Profiler.Start("updateLighting");
                        world.Lighting.DoLightingUpdates();
                        if (options.DebugMode) Profiler.Stop("updateLighting");
                    }

                    if (!Keyboard.isKeyDown(Keyboard.KEY_F7))
                    {
                        if (options.DebugMode) Profiler.Start("wait");
                        Display.update();
                        if (options.DebugMode) Profiler.Stop("wait");
                    }

                    if (player != null && player.isInsideWall())
                    {
                        options.CameraMode = EnumCameraMode.FirstPerson;
                    }

                    if (!skipRenderWorld)
                    {
                        playerController?.setPartialTime(Timer.renderPartialTicks);

                        if (options.DebugMode)
                        {
                            Profiler.PushGroup("render");
                            TextureStats.StartFrame();
                        }
                        gameRenderer.onFrameUpdate(Timer.renderPartialTicks);
                        if (options.DebugMode)
                        {
                            TextureStats.EndFrame();
                            Profiler.PopGroup();
                        }
                    }

                    if (imGuiController != null && Timer.DeltaTime > 0.0f && options.ShowDebugInfo && options.DebugMode)
                    {
                        imGuiController.Update(Timer.DeltaTime);
                        ProfilerRenderer.Draw();
                        ProfilerRenderer.DrawGraph();

                        ImGui.Begin("Render Info");
                        ImGui.Text($"Chunks Total: {terrainRenderer.chunkRenderer.TotalChunks}");
                        ImGui.Text($"Chunks Frustum: {terrainRenderer.chunkRenderer.ChunksInFrustum}");
                        ImGui.Text($"Chunks Occluded: {terrainRenderer.chunkRenderer.ChunksOccluded}");
                        ImGui.Text($"Chunks Rendered: {terrainRenderer.chunkRenderer.ChunksRendered}");
                        ImGui.Separator();
                        ImGui.Text($"Chunk Vertex Buffer Allocated MB: {VertexBuffer<ChunkVertex>.Allocated / 1000000.0}");
                        ImGui.Text($"ChunkMeshVersion Allocated: {ChunkMeshVersion.TotalAllocated}");
                        ImGui.Text($"ChunkMeshVersion Released: {ChunkMeshVersion.TotalReleased}");

                        terrainRenderer.chunkRenderer.GetMeshSizeStats(out int minSize, out int maxSize, out int avgSize, out Dictionary<int, int> buckets);
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
                            string json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(Path.Combine(getBetaSharpDir(), "mesh_stats.json"), json);
                            _logger.LogInformation($"Exported mesh stats to {Path.Combine(getBetaSharpDir(), "mesh_stats.json")}");
                        }

                        ImGui.Separator();
                        ImGui.Text($"Texture Binds: {TextureStats.BindsLastFrame} (Avg: {TextureStats.AverageBindsPerFrame:F1}/f)");
                        ImGui.Text($"Active Textures: {GLTexture.ActiveTextureCount}");
                        ImGui.End();

                        imGuiController.Render();
                    }

                    if (!Display.isActive())
                    {
                        if (fullscreen)
                        {
                            toggleFullscreen();
                        }

                        Thread.Sleep(10);
                    }

                    if (options.ShowDebugInfo && options.ShowDebugGraphOption.Value)
                    {
                        displayDebugInfo(tickElapsedTime);
                    }
                    else
                    {
                        prevFrameTime = Stopwatch.GetTimestamp();
                    }

                    guiAchievement.updateAchievementWindow();

                    if (Keyboard.isKeyDown(Keyboard.KEY_F7))
                    {
                        Display.update();
                    }

                    screenshotListener();

                    if (Display.wasResized())
                    {
                        displayWidth = Display.getWidth();
                        displayHeight = Display.getHeight();
                        if (displayWidth <= 0)
                        {
                            displayWidth = 1;
                        }

                        if (displayHeight <= 0)
                        {
                            displayHeight = 1;
                        }

                        resize(displayWidth, displayHeight);
                    }

                    checkGLError("Post render");
                    ++frameCounter;

                    isGamePaused = (!isMultiplayerWorld() || internalServer != null) && (currentScreen?.PausesGame ?? false);

                    for (;
                         DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 >= lastFpsCheckTime + 1000L;
                         frameCounter = 0)
                    {
                        debug = frameCounter + " fps";
                        lastFpsCheckTime += 1000L;
                    }
                }
                catch (OutOfMemoryException)
                {
                    crashCleanup();
                    displayGuiScreen(new GuiErrorScreen());
                }
                finally
                {
                    long frameEndNano = Stopwatch.GetTimestamp();
                    double thisFrameTimeMs = (frameEndNano - frameStartNano) / 1000000.0;
                    _debugTelemetry.RecordFrameTime(thisFrameTimeMs);

                    if (options.DebugMode)
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

                            int fpsLimit = 30 + (int)(options.LimitFramerate * 210.0f);
                            double msPerFrameTarget = fpsLimit == 240 ? 16.666 : (1000.0 / fpsLimit);
                            Profiler.LagSpikeThresholdMs = msPerFrameTarget * 2.0;
                            Profiler.DetectLagSpike(thisFrameTimeMs, string.IsNullOrEmpty(gcContext) ? debug : $"{debug} - {gcContext}", true);
                        }
                    }
                }
            }
        }
        catch (BetaSharpShutdownException) { }
        catch (Exception unexpectedException)
        {
            crashCleanup();
            onBetaSharpCrash(unexpectedException);
        }
        finally
        {
            ShutdownBetaSharpApplet();
        }
    }

    public void crashCleanup()
    {
        try
        {
            changeWorld(null);
        }
        catch (Exception)
        {
        }
    }

    private void screenshotListener()
    {
        if (Keyboard.isKeyDown(Keyboard.KEY_F2))
        {
            if (!isTakingScreenshot)
            {
                isTakingScreenshot = true;
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
                string result = ScreenShotHelper.saveScreenshot(gameDataDir, displayWidth, displayHeight, pixels);
                ingameGUI.AddChatMessage(result);
            }
        }
        else
        {
            isTakingScreenshot = false;
        }
    }

    private void displayDebugInfo(long tickElapsedTime)
    {
        long targetFrameTime = 16666666L;
        if (prevFrameTime == -1L)
        {
            prevFrameTime = Stopwatch.GetTimestamp();
        }

        long currentNanoTime = Stopwatch.GetTimestamp();
        tickTimes[numRecordedFrameTimes & frameTimes.Length - 1] = tickElapsedTime;
        frameTimes[numRecordedFrameTimes++ & frameTimes.Length - 1] = currentNanoTime - prevFrameTime;
        prevFrameTime = currentNanoTime;
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0D, (double)displayWidth, (double)displayHeight, 0.0D, 1000.0D, 3000.0D);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Translate(0.0F, 0.0F, -2000.0F);
        GLManager.GL.LineWidth(1.0F);
        GLManager.GL.Disable(GLEnum.Texture2D);
        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawing(7);
        int barHeightPixels = (int)(targetFrameTime / 200000L);
        tessellator.setColorOpaque_I(0x20000000); // BUG: tries to set alpha, which is ignored by setColorOpaque_I
        tessellator.addVertex(0.0D, (double)(displayHeight - barHeightPixels), 0.0D);
        tessellator.addVertex(0.0D, (double)displayHeight, 0.0D);
        tessellator.addVertex((double)frameTimes.Length, (double)displayHeight, 0.0D);
        tessellator.addVertex((double)frameTimes.Length, (double)(displayHeight - barHeightPixels), 0.0D);
        tessellator.setColorOpaque_I(0x20200000); // BUG: tries to set alpha, which is ignored by setColorOpaque_I
        tessellator.addVertex(0.0D, (double)(displayHeight - barHeightPixels * 2), 0.0D);
        tessellator.addVertex(0.0D, (double)(displayHeight - barHeightPixels), 0.0D);
        tessellator.addVertex((double)frameTimes.Length, (double)(displayHeight - barHeightPixels), 0.0D);
        tessellator.addVertex((double)frameTimes.Length, (double)(displayHeight - barHeightPixels * 2), 0.0D);
        tessellator.draw();
        long totalFrameTimesSum = 0L;

        int averageFrameTimePixels;
        for (averageFrameTimePixels = 0; averageFrameTimePixels < frameTimes.Length; ++averageFrameTimePixels)
        {
            totalFrameTimesSum += frameTimes[averageFrameTimePixels];
        }

        averageFrameTimePixels = (int)(totalFrameTimesSum / 200000L / (long)frameTimes.Length);
        tessellator.startDrawing(7);
        tessellator.setColorOpaque_I(0x20400000); // BUG: tries to set alpha, which is ignored by setColorOpaque_I
        tessellator.addVertex(0.0D, (double)(displayHeight - averageFrameTimePixels), 0.0D);
        tessellator.addVertex(0.0D, (double)displayHeight, 0.0D);
        tessellator.addVertex((double)frameTimes.Length, (double)displayHeight, 0.0D);
        tessellator.addVertex((double)frameTimes.Length, (double)(displayHeight - averageFrameTimePixels), 0.0D);
        tessellator.draw();
        tessellator.startDrawing(1);

        for (int frameIndex = 0; frameIndex < frameTimes.Length; ++frameIndex)
        {
            int colorBrightnessPercent = (frameIndex - numRecordedFrameTimes & frameTimes.Length - 1) * 255 / frameTimes.Length;
            int colorBrightness = colorBrightnessPercent * colorBrightnessPercent / 255;
            colorBrightness = colorBrightness * colorBrightness / 255;
            int colorValue = colorBrightness * colorBrightness / 255;
            colorValue = colorValue * colorValue / 255;
            if (frameTimes[frameIndex] > targetFrameTime)
            {
                tessellator.setColorOpaque_I(unchecked((int)(0xFF000000u + (uint)colorBrightness * 65536u)));
            }
            else
            {
                tessellator.setColorOpaque_I(unchecked((int)(0xFF000000u + (uint)colorBrightness * 256u)));
            }

            long frameTimePixels = frameTimes[frameIndex] / 200000L;
            long tickTimePixels = tickTimes[frameIndex] / 200000L;
            tessellator.addVertex((double)((float)frameIndex + 0.5F), (double)((float)((long)displayHeight - frameTimePixels) + 0.5F),
                0.0D);
            tessellator.addVertex((double)((float)frameIndex + 0.5F), (double)((float)displayHeight + 0.5F), 0.0D);
            tessellator.setColorOpaque_I(unchecked((int)(0xFF000000u + (uint)colorBrightness * 65536u + (uint)colorBrightness * 256u + (uint)colorBrightness * 1u)));
            tessellator.addVertex((double)((float)frameIndex + 0.5F), (double)((float)((long)displayHeight - frameTimePixels) + 0.5F),
                0.0D);
            tessellator.addVertex((double)((float)frameIndex + 0.5F),
                (double)((float)((long)displayHeight - (frameTimePixels - tickTimePixels)) + 0.5F), 0.0D);
        }

        tessellator.draw();
        GLManager.GL.Enable(GLEnum.Texture2D);
    }

    public void shutdown()
    {
        running = false;
    }

    public void setIngameFocus()
    {
        if (Display.isActive())
        {
            if (!inGameHasFocus)
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                inGameHasFocus = true;
                mouseHelper.grabMouseCursor();
                displayGuiScreen((GuiScreen)null);
                leftClickCounter = 10000;
                MouseTicksRan = TicksRan + 10000;
            }
        }
    }

    public void stopInternalServer()
    {
        if (internalServer != null)
        {
            internalServer.Stop();
            while (!internalServer.stopped)
            {
                Thread.Sleep(1);
            }
            internalServer = null;
        }
    }

    public void setIngameNotInFocus()
    {
        if (inGameHasFocus)
        {
            player?.resetPlayerKeyState();

            inGameHasFocus = false;
            GCSettings.LatencyMode = GCLatencyMode.Batch;
            mouseHelper.ungrabMouseCursor();
            Mouse.setCursorVisible(!isControllerMode);
        }
    }

    public void displayInGameMenu()
    {
        if (currentScreen == null)
        {
            displayGuiScreen(new GuiIngameMenu());
        }
    }

    private void func_6254_a(int mouseButton, bool isHoldingMouse)
    {
        if (!playerController.IsTestPlayer)
        {
            if (!isHoldingMouse)
            {
                leftClickCounter = 0;
            }

            if (mouseButton != 0 || leftClickCounter <= 0)
            {
                if (isHoldingMouse && objectMouseOver.Type != HitResultType.MISS && objectMouseOver.Type == HitResultType.TILE &&
                    mouseButton == 0)
                {
                    int blockX = objectMouseOver.BlockX;
                    int blockY = objectMouseOver.BlockY;
                    int blockZ = objectMouseOver.BlockZ;
                    playerController.sendBlockRemoving(blockX, blockY, blockZ, objectMouseOver.Side);
                    particleManager.addBlockHitEffects(blockX, blockY, blockZ, objectMouseOver.Side);
                }
                else
                {
                    playerController.resetBlockRemoving();
                }
            }
        }
    }

    public void ClickMouse(int mouseButton)
    {
        if (mouseButton != 0 || leftClickCounter <= 0)
        {
            if (mouseButton == 0)
            {
                player.swingHand();
            }

            bool shouldPerformSecondaryAction = true;
            if (objectMouseOver.Type == HitResultType.MISS)
            {
                if (mouseButton == 0)
                {
                    leftClickCounter = 10;
                }
            }
            else if (objectMouseOver.Type == HitResultType.ENTITY)
            {
                if (mouseButton == 0)
                {
                    playerController.attackEntity(player, objectMouseOver.Entity);
                }

                if (mouseButton == 1)
                {
                    playerController.interactWithEntity(player, objectMouseOver.Entity);
                }
            }
            else if (objectMouseOver.Type == HitResultType.TILE)
            {
                int blockX = objectMouseOver.BlockX;
                int blockY = objectMouseOver.BlockY;
                int blockZ = objectMouseOver.BlockZ;
                int blockSide = objectMouseOver.Side;
                if (mouseButton == 0)
                {
                    playerController.clickBlock(blockX, blockY, blockZ, objectMouseOver.Side);
                }
                else
                {
                    ItemStack selectedItem = player.inventory.getSelectedItem();
                    int itemCountBefore = selectedItem != null ? selectedItem.count : 0;
                    if (playerController.sendPlaceBlock(player, world, selectedItem, blockX, blockY, blockZ, blockSide))
                    {
                        shouldPerformSecondaryAction = false;
                        player.swingHand();
                    }

                    if (selectedItem == null)
                    {
                        return;
                    }

                    if (selectedItem.count == 0)
                    {
                        player.inventory.main[player.inventory.selectedSlot] = null;
                    }
                    else if (selectedItem.count != itemCountBefore)
                    {
                        gameRenderer.itemRenderer.func_9449_b();
                    }
                }
            }

            if (shouldPerformSecondaryAction && mouseButton == 1)
            {
                ItemStack selectedItem = player.inventory.getSelectedItem();
                if (selectedItem != null && playerController.sendUseItem(player, world, selectedItem))
                {
                    gameRenderer.itemRenderer.func_9450_c();
                }
            }
        }
    }

    public void toggleFullscreen()
    {
        try
        {
            fullscreen = !fullscreen;
            if (fullscreen)
            {
                tempDisplayWidth = displayWidth;
                tempDisplayHeight = displayHeight;

                Display.setDisplayMode(Display.getDesktopDisplayMode());
                Display.setFullscreen(true);
                displayWidth = Display.getDisplayMode().getWidth();
                displayHeight = Display.getDisplayMode().getHeight();
                if (displayWidth <= 0)
                {
                    displayWidth = 1;
                }

                if (displayHeight <= 0)
                {
                    displayHeight = 1;
                }
            }
            else
            {
                Display.setFullscreen(false);
                if (tempDisplayWidth > 0 && tempDisplayHeight > 0)
                {
                    Display.setDisplayMode(new DisplayMode(tempDisplayWidth, tempDisplayHeight));
                    displayWidth = tempDisplayWidth;
                    displayHeight = tempDisplayHeight;
                }
                else
                {
                    Display.setDisplayMode(new DisplayMode(854, 480));
                    displayWidth = 854;
                    displayHeight = 480;
                }

                if (displayWidth <= 0)
                {
                    displayWidth = 1;
                }

                if (displayHeight <= 0)
                {
                    displayHeight = 1;
                }

                // Center the window
                DisplayMode desktopMode = Display.getDesktopDisplayMode();
                int centerX = (desktopMode.getWidth() - displayWidth) / 2;
                int centerY = (desktopMode.getHeight() - displayHeight) / 2;
                Display.setLocation(centerX, centerY);
            }

            if (currentScreen != null)
            {
                resize(displayWidth, displayHeight);
            }

            Display.update();
        }
        catch (Exception displayException)
        {
            _logger.LogError(displayException.ToString());
        }
    }

    private void resize(int newWidth, int newHeight)
    {
        if (newWidth <= 0)
        {
            newWidth = 1;
        }

        if (newHeight <= 0)
        {
            newHeight = 1;
        }

        displayWidth = newWidth;
        displayHeight = newHeight;
        Mouse.setDisplayDimensions(displayWidth, displayHeight);

        if (currentScreen != null)
        {
            ScaledResolution scaledResolution = new(options, newWidth, newHeight);
            int scaledWidth = scaledResolution.ScaledWidth;
            int scaledHeight = scaledResolution.ScaledHeight;
            currentScreen.SetWorldAndResolution(this, scaledWidth, scaledHeight);
        }

        PostProcessManager.Resize(Display.getFramebufferWidth(), Display.getFramebufferHeight());
    }

    public void ClickMiddleMouseButton()
    {
        if (objectMouseOver.Type != HitResultType.MISS)
        {
            int blockId = world.Reader.GetBlockId(objectMouseOver.BlockX, objectMouseOver.BlockY, objectMouseOver.BlockZ);
            if (blockId == Block.GrassBlock.id)
            {
                blockId = Block.Dirt.id;
            }

            if (blockId == Block.DoubleSlab.id)
            {
                blockId = Block.Slab.id;
            }

            if (blockId == Block.Bedrock.id)
            {
                blockId = Block.Stone.id;
            }

            player.inventory.setCurrentItem(blockId, false);
        }
    }

    public void runTick(float partialTicks)
    {
        Profiler.PushGroup("runTick");

        Profiler.Start("statFileWriter.SyncStatsIfReady");
        statFileWriter.SyncStatsIfReady();
        Profiler.Stop("statFileWriter.SyncStatsIfReady");

        if (!inGameHasFocus && world == null && internalServer == null)
        {
            if (options.MenuMusic)
            {
                sndManager.PlayRandomMusicIfReady(DefaultMusicCategories.Menu);
            }
            else
            {
                sndManager.StopMusic(DefaultMusicCategories.Menu);
            }
        }



        Profiler.Start("ingameGUI.updateTick");
        ingameGUI.UpdateTick();
        Profiler.Stop("ingameGUI.updateTick");
        gameRenderer.UpdateTargetedEntity(1.0F);

        gameRenderer.tick(partialTicks);

        Profiler.Start("chunkProviderLoadOrGenerateSetCurrentChunkOver");

        Profiler.Stop("chunkProviderLoadOrGenerateSetCurrentChunkOver");

        Profiler.Start("playerControllerUpdate");
        if (!isGamePaused && world != null)
        {
            playerController.updateController();
        }

        Profiler.Stop("playerControllerUpdate");

        Profiler.Start("updateDynamicTextures");
        textureManager.BindTexture(textureManager.GetTextureId("/terrain.png"));
        if (!isGamePaused)
        {
            textureManager.Tick();
        }

        Profiler.Stop("updateDynamicTextures");

        if (currentScreen == null && player != null)
        {
            if (player.health <= 0)
            {
                displayGuiScreen((GuiScreen)null);
            }
            else if (player.isSleeping() && world != null && world.IsRemote)
            {
                displayGuiScreen(new GuiSleepMP());
            }
        }
        else if (currentScreen != null && currentScreen is GuiSleepMP && !player.isSleeping())
        {
            displayGuiScreen((GuiScreen)null);
        }

        if (currentScreen != null)
        {
            leftClickCounter = 10000;
            MouseTicksRan = TicksRan + 10000;
        }

        if (currentScreen != null)
        {
            currentScreen.HandleInput();
            if (currentScreen != null)
            {
                currentScreen.ParticlesGui.updateParticles();
                currentScreen.UpdateScreen();
            }
        }

        if (currentScreen == null || currentScreen.AllowUserInput)
        {
            processInputEvents();
        }

        if (world != null)
        {
            if (player != null)
            {
                ++joinPlayerCounter;
                if (joinPlayerCounter == 30)
                {
                    joinPlayerCounter = 0;
                    world.Entities.LoadChunksNearEntity(player);
                }
            }

            world.SetDifficulty(options.Difficulty);
            if (internalServer != null)
            {
                internalServer.SetDifficulty(options.Difficulty);
            }

            if (world.IsRemote)
            {
                world.SetDifficulty(3);
            }

            Profiler.Start("entityRendererUpdate");
            if (!isGamePaused)
            {
                gameRenderer.updateCamera();
            }

            Profiler.Stop("entityRendererUpdate");

            if (!isGamePaused)
            {
                terrainRenderer.updateClouds();
            }

            Profiler.PushGroup("theWorldUpdateEntities");
            if (!isGamePaused)
            {
                if (world.Environment.LightningTicksLeft > 0)
                {
                    --world.Environment.LightningTicksLeft;
                }

                world.Entities.TickEntities();
            }

            Profiler.PopGroup();

            Profiler.PushGroup("theWorld.tick");
            if (!isGamePaused || (isMultiplayerWorld() && internalServer == null))
            {
                world.allowSpawning(options.Difficulty > 0, true);
                world.Tick();
            }

            Profiler.PopGroup();

            if (!isGamePaused && world != null)
            {
                world.displayTick(MathHelper.Floor(player.x),
                    MathHelper.Floor(player.y), MathHelper.Floor(player.z));
            }

            if (!isGamePaused)
            {
                particleManager.updateEffects();
            }
        }

        systemTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
        Profiler.PopGroup();
    }

    private void processInputEvents()
    {
        while (Mouse.next())
        {
            long timeSinceLastMouseEvent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 - systemTime;
            if (Mouse.getEventDX() != 0 || Mouse.getEventDY() != 0)
            {
                isControllerMode = false;
                Mouse.setCursorVisible(true);
            }

            if (timeSinceLastMouseEvent <= 200L)
            {
                int mouseWheelDelta = Mouse.getEventDWheel();
                if (mouseWheelDelta != 0)
                {
                    isControllerMode = false;
                    Mouse.setCursorVisible(true);

                    bool zoomHeld = currentScreen == null && inGameHasFocus && Keyboard.isKeyDown(options.KeyBindZoom.keyCode);
                    if (zoomHeld)
                    {
                        int mouseWheelDirection = mouseWheelDelta > 0 ? 1 : -1;
                        if (mouseWheelDirection > 0)
                        {
                            options.ZoomScale *= 1.08F;
                        }
                        else
                        {
                            options.ZoomScale /= 1.08F;
                        }

                        options.ZoomScale = System.Math.Clamp(options.ZoomScale, 1.25F, 20.0F);
                    }
                    else
                    {
                        player.inventory.changeCurrentItem(mouseWheelDelta);
                        if (options.InvertScrolling)
                        {
                            if (mouseWheelDelta > 0)
                            {
                                mouseWheelDelta = 1;
                            }

                            if (mouseWheelDelta < 0)
                            {
                                mouseWheelDelta = -1;
                            }

                            options.AmountScrolled += (float)mouseWheelDelta * 0.25F;
                        }
                    }
                }

                if (currentScreen == null)
                {
                    if (!inGameHasFocus && Mouse.getEventButtonState())
                    {
                        setIngameFocus();
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
                    currentScreen?.HandleMouseInput();
                }
            }
        }

        if (leftClickCounter > 0)
        {
            --leftClickCounter;
        }

        while (Keyboard.Next())
        {
            player.handleKeyPress(Keyboard.getEventKey(), Keyboard.getEventKeyState());

            if (Keyboard.getEventKeyState())
            {
                if (Keyboard.getEventKey() == Keyboard.KEY_F11)
                {
                    toggleFullscreen();
                }
                else
                {
                    if (currentScreen != null)
                    {
                        currentScreen.HandleKeyboardInput();
                    }
                    else
                    {
                        if (Keyboard.getEventKey() == Keyboard.KEY_ESCAPE)
                        {
                            displayInGameMenu();
                        }

                        if (Keyboard.getEventKey() == Keyboard.KEY_S && Keyboard.isKeyDown(Keyboard.KEY_F3))
                        {
                            forceReload();
                        }

                        if (Keyboard.getEventKey() == Keyboard.KEY_D && Keyboard.isKeyDown(Keyboard.KEY_F3))
                        {
                            ingameGUI.ClearChatMessages();
                        }

                        if (Keyboard.getEventKey() == Keyboard.KEY_C && Keyboard.isKeyDown(Keyboard.KEY_F3))
                        {
                            throw new Exception("Simulated crash triggered by pressing F3 + C");
                        }

                        if (Keyboard.getEventKey() == Keyboard.KEY_F1)
                        {
                            options.HideGUI = !options.HideGUI;
                        }

                        if (Keyboard.getEventKey() == Keyboard.KEY_F3)
                        {
                            options.ShowDebugInfo = !options.ShowDebugInfo;
                        }

                        if (Keyboard.getEventKey() == Keyboard.KEY_F5)
                        {
                            options.CameraMode = (EnumCameraMode)((int)(options.CameraMode + 2) % 3);
                        }

                        if (Keyboard.getEventKey() == Keyboard.KEY_F8)
                        {
                            options.SmoothCamera = !options.SmoothCamera;
                        }

                        if (Keyboard.getEventKey() == Keyboard.KEY_F7)
                        {
                            ShowChunkBorders = !ShowChunkBorders;
                        }

                        if (Keyboard.getEventKey() == options.KeyBindInventory.keyCode)
                        {
                            displayGuiScreen(new GuiInventory(player));
                        }

                        if (Keyboard.getEventKey() == options.KeyBindDrop.keyCode)
                        {
                            player.dropSelectedItem();
                        }

                        if (Keyboard.getEventKey() == options.KeyBindChat.keyCode)
                        {
                            displayGuiScreen(new GuiChat());
                        }

                        if (Keyboard.getEventKey() == options.KeyBindCommand.keyCode)
                        {
                            displayGuiScreen(new GuiChat("/"));
                        }
                    }

                    for (int slotIndex = 0; slotIndex < 9; ++slotIndex)
                    {
                        if (Keyboard.getEventKey() == Keyboard.KEY_1 + slotIndex)
                        {
                            player.inventory.selectedSlot = slotIndex;
                        }
                    }

                    if (Keyboard.getEventKey() == options.KeyBindToggleFog.keyCode)
                    {
                        options.RenderDistanceOption.Value = System.Math.Clamp(options.RenderDistanceOption.Value + (!Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) && !Keyboard.isKeyDown(Keyboard.KEY_RSHIFT) ? 1.0f / 28.0f : -1.0f / 28.0f), 0.0f, 1.0f);
                    }
                }
            }
        }


        ControllerManager.UpdateGui(currentScreen);

        ControllerManager.UpdateInGame(Timer.renderPartialTicks);

        if (currentScreen == null)
        {
            if (Mouse.isButtonDown(0) && (float)(TicksRan - MouseTicksRan) >= Timer.ticksPerSecond / 4.0F &&
                inGameHasFocus)
            {
                ClickMouse(0);
                MouseTicksRan = TicksRan;
            }

            if (Mouse.isButtonDown(1) && (float)(TicksRan - MouseTicksRan) >= Timer.ticksPerSecond / 4.0F &&
                inGameHasFocus)
            {
                ClickMouse(1);
                MouseTicksRan = TicksRan;
            }
        }

        func_6254_a(0, currentScreen == null && (Mouse.isButtonDown(0) || Controller.RightTrigger > 0.5f) && inGameHasFocus);
    }

    private void forceReload()
    {
        _logger.LogInformation("FORCING RELOAD!");
        sndManager = new SoundManager();
        sndManager.LoadSoundSettings(options);
        DefaultMusicCategories.Register(sndManager);
    }

    public bool isMultiplayerWorld()
    {
        return world != null && world.IsRemote;
    }

    public void startWorld(string worldName, string mainMenuText, WorldSettings settings)
    {
        changeWorld(null);
        displayGuiScreen(new GuiLevelLoading(worldName, settings));
    }

    public void changeWorld(World newWorld, string loadingText = "", EntityPlayer targetEntity = null)
    {
        statFileWriter.Tick();
        statFileWriter.SyncStats();
        camera = null;
        loadingScreen.printText(loadingText);
        loadingScreen.progressStage("");
        sndManager.PlayStreaming(null!, 0.0F, 0.0F, 0.0F, 0.0F, 0.0F);

        world = newWorld;
        if (newWorld != null)
        {
            playerController.func_717_a(newWorld);
            if (!isMultiplayerWorld())
            {
                if (targetEntity == null)
                {
                    player = (ClientPlayerEntity)newWorld.GetPlayerForProxy(typeof(ClientPlayerEntity));
                }
            }
            else if (player != null)
            {
                player.teleportToTop();
                newWorld?.Entities.SpawnEntity(player);
            }

            if (player == null)
            {
                player = (ClientPlayerEntity)playerController.createPlayer(newWorld);
                player.teleportToTop();
                playerController.flipPlayer(player);
            }

            player.movementInput = new MovementInputFromOptions(options);
            terrainRenderer?.changeWorld(newWorld);

            particleManager?.clearEffects(newWorld);

            playerController.fillHotbar(player);
            if (targetEntity != null)
            {
                newWorld.SaveWorldData();
            }

            newWorld.AddPlayer(player);

            skinManager.RequestDownload(player.name);

            if (newWorld.IsNewWorld)
            {
                newWorld.SavingProgress(loadingScreen);
            }

            camera = player;
        }
        else
        {
            player = null;
        }

        systemTime = 0L;
    }

    private void showText(string loadingText)
    {
        loadingScreen.printText(loadingText);
        loadingScreen.progressStage("Building terrain");
        short loadingRadius = 128;
        int loadedChunkCount = 0;
        int totalChunksToLoad = loadingRadius * 2 / 16 + 1;
        totalChunksToLoad *= totalChunksToLoad;
        Vec3i centerPos = world.Properties.GetSpawnPos();
        if (player != null)
        {
            centerPos.X = (int)player.x;
            centerPos.Z = (int)player.z;
        }

        for (int xOffset = -loadingRadius; xOffset <= loadingRadius; xOffset += 16)
        {
            for (int zOffset = -loadingRadius; zOffset <= loadingRadius; zOffset += 16)
            {
                loadingScreen.setLoadingProgress(loadedChunkCount++ * 100 / totalChunksToLoad);
                world.Reader.GetBlockId(centerPos.X + xOffset, 64, centerPos.Z + zOffset);

                while (world.Lighting.DoLightingUpdates())
                {
                }
            }
        }

        loadingScreen.progressStage("Simulating world for a bit");
        world.TickChunks();
    }

    public void installResource(string resourcePath, FileInfo resourceFile)
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
            sndManager.AddSound(resourcePath, resourceFile);
        }
        else if (category.Equals("newsound", StringComparison.OrdinalIgnoreCase))
        {
            sndManager.AddSound(resourcePath, resourceFile);
        }
        else if (category.Equals("streaming", StringComparison.OrdinalIgnoreCase))
        {
            sndManager.AddStreaming(resourcePath, resourceFile);
        }
        else if (category.Equals("music", StringComparison.OrdinalIgnoreCase))
        {
            sndManager.AddMusic(DefaultMusicCategories.Game, resourcePath, resourceFile);
        }
        else if (category.Equals("newmusic", StringComparison.OrdinalIgnoreCase))
        {
            sndManager.AddMusic(DefaultMusicCategories.Game, resourcePath, resourceFile);
        }
        else if (category.Equals("custom", StringComparison.OrdinalIgnoreCase))
        {
            int subSlash = resourcePath.IndexOf("/");
            string subCategory = resourcePath.Substring(0, subSlash);
            resourcePath = resourcePath.Substring(subSlash + 1);

            if (subCategory.Equals("music", StringComparison.OrdinalIgnoreCase))
            {
                sndManager.AddMusic(DefaultMusicCategories.Menu, resourcePath, resourceFile);
            }
        }
    }


    public string getWorldDebugInfo()
    {
        return world.GetDebugInfo();
    }

    public string getParticleDebugInfo()
    {
        return "Particles: " + particleManager.getStatistics();
    }

    internal DebugSystemSnapshot GetDebugSystemSnapshot()
    {
        return _debugTelemetry.SystemSnapshot;
    }

    internal DebugFrameStatsSnapshot GetDebugFrameStatsSnapshot()
    {
        return _debugTelemetry.GetFrameStatsSnapshot();
    }

    public void respawn(bool ignoreSpawnPosition, int newDimensionId)
    {
        Vec3i? playerSpawnPos = null;
        Vec3i? respawnPos = null;

        if (player is not null && !ignoreSpawnPosition)
        {
            playerSpawnPos = player.getSpawnPos();

            if (playerSpawnPos is not null)
            {
                respawnPos = EntityPlayer.findRespawnPosition(world, playerSpawnPos);

                if (respawnPos is null)
                {
                    player.sendMessage("tile.bed.notValid");
                }
            }
        }

        bool useBedSpawn = respawnPos is not null;
        Vec3i finalRespawnPos = respawnPos ?? world.Properties.GetSpawnPos();

        world.UpdateSpawnPosition();
        world.Entities.UpdateEntityLists();

        int previousPlayerId = 0;

        if (player is not null)
        {
            previousPlayerId = player.id;
            world.Entities.Remove(player);
        }

        camera = null;
        player = (ClientPlayerEntity)playerController.createPlayer(world);
        player.dimensionId = newDimensionId;
        camera = player;

        player.teleportToTop();

        if (useBedSpawn)
        {
            player.setSpawnPos(playerSpawnPos);
            player.setPositionAndAnglesKeepPrevAngles(
                finalRespawnPos.X + 0.5,
                finalRespawnPos.Y + 0.1,
                finalRespawnPos.Z + 0.5,
                0.0F,
                0.0F);
        }

        playerController.flipPlayer(player);
        world.AddPlayer(player);
        player.movementInput = new MovementInputFromOptions(options);
        player.id = previousPlayerId;
        player.spawn();
        playerController.fillHotbar(player);

        showText("Respawning");

        if (currentScreen is GuiGameOver)
        {
            displayGuiScreen(null);
        }
    }

    private static void StartMainThread(string playerName, string sessionToken)
    {
        Thread.CurrentThread.Name = "BetaSharp Main Thread";

        BetaSharp game = new(850, 480, false);

        if (playerName != null && sessionToken != null)
        {
            game.session = new Session(playerName, sessionToken);

            if (sessionToken == "-")
            {
                hasPaidCheckTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    ;
            }
        }
        else
        {
            throw new Exception("Player name and session token were not provided!");
        }

        game.Run();
    }

    public ClientNetworkHandler getSendQueue()
    {
        return player is EntityClientPlayerMP ? ((EntityClientPlayerMP)player).sendQueue : null;
    }

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

    public static bool isGuiEnabled()
    {
        return Instance == null || !Instance.options.HideGUI;
    }

    public static bool isFancyGraphicsEnabled()
    {
        return Instance != null;
    }

    public static bool isAmbientOcclusionEnabled()
    {
        return Instance != null;
    }

    public static bool isDebugInfoEnabled()
    {
        return Instance != null && Instance.options.ShowDebugInfo;
    }
}
