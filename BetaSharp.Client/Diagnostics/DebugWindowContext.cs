using BetaSharp.Client.Entities;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Sound;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Screens.InGame;
using BetaSharp.Util.Hit;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Diagnostics;

/// <summary>
/// Aggregates the inputs required by the debug window system so that individual windows
/// are not coupled directly to <see cref="BetaSharp"/>.
/// </summary>
/// <remarks>
/// <para>
/// Choose the right data source for each debug window:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><see cref="MetricRegistry"/></term>
///     <description>
///     Use for simple counters or scalars that are written frequently (every frame or every tick)
///     by hot code paths elsewhere in the engine. The registry is a passive store — reading from
///     it in <c>OnDraw</c> has no side effects and no allocation.
///     </description>
///   </item>
///   <item>
///     <term><see cref="DebugWindowContext"/> (this class)</term>
///     <description>
///     Use when you need to call methods, traverse object graphs, or access state that isn't a
///     simple scalar — e.g. querying the world for biome data, walking the UI element tree, or
///     reading structured snapshots like <see cref="DebugSystemSnapshot"/>.
///     </description>
///   </item>
/// </list>
/// </remarks>
internal sealed class DebugWindowContext(BetaSharp game)
{
    public RendererBackendKind RequestedRendererBackend => game.RequestedRendererBackend;
    public RendererBackendKind ActiveRendererBackend => game.ActiveRendererBackend;
    public RendererBackendKind DisplayRendererBackend => Display.ActiveRendererBackend;
    public bool DisplaySupportsWindowBufferSwap => Display.SupportsWindowBufferSwap;
    public bool DisplayHasOpenGlContext => Display.HasOpenGlContext;
    public RendererBackendKind ImGuiRendererBackend => game.ImGuiRendererBackend;
    public RendererBackendKind PresentationRendererBackend => game.PresentationRendererBackend;
    public int PresentationTargetWidth => game.PresentationTargetWidth;
    public int PresentationTargetHeight => game.PresentationTargetHeight;
    public bool IsPresentationBlitSkipped => game.IsPresentationBlitSkipped;
    public bool IsRendererRuntimeInitialized => game.IsRendererRuntimeInitialized;
    public bool SupportsLegacyOpenGlRenderPath => game.SupportsLegacyOpenGlRenderPath;
    public bool SupportsScreenshotCapture => game.SupportsScreenshotCapture;
    public string? RendererFallbackReason => game.RendererFallbackReason;

    public World? World => game.World;
    public ClientPlayerEntity? Player => game.Player;
    public HitResult ObjectMouseOver => game.ObjectMouseOver;
    public DebugSystemSnapshot DebugSystemSnapshot => game.DebugSystemSnapshot;
    public UIScreen? CurrentScreen => game.CurrentScreen;
    public HUD HUD => game.HUD;
    public UIContext UIContext => game.UIContext;
    public SoundManager SoundManager => game.SoundManager;

    /// <summary>
    /// The top-left screen position (in ImGui/window pixels) of the game viewport when the
    /// debug menu is open, or <see cref="System.Numerics.Vector2.Zero"/> otherwise.
    /// Used by overlays that draw into the foreground draw list.
    /// </summary>
    public System.Numerics.Vector2 DebugViewportScreenPos => game.DebugViewportScreenPos;
}
