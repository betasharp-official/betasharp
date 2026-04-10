using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Client.UI.Screens;
using Silk.NET.Maths;

namespace BetaSharp.Client.UI;

public sealed class UIContext(
    GameOptions options,
    ITextRenderer textRenderer,
    ITextureManager textureManager,
    IEntityRenderDispatcher entityRenderDispatcher,
    IBlockEntityRenderDispatcher blockEntityRenderDispatcher,
    IUiRenderBackend uiRenderBackend,
    Action playClickSound,
    Func<Vector2D<int>> displaySize,
    Func<Vector2D<int>> inputDisplaySize,
    IControllerState controllerState,
    VirtualCursor virtualCursor,
    Timer timer,
    IScreenNavigator navigator,
    Func<bool> hasWorld,
    Func<Vector2D<int>> mouseOffset
    )
{

    public GameOptions Options => options;
    public ITextRenderer TextRenderer => textRenderer;
    public ITextureManager TextureManager => textureManager;
    public IEntityRenderDispatcher EntityRenderDispatcher => entityRenderDispatcher;
    public IBlockEntityRenderDispatcher BlockEntityRenderDispatcher => blockEntityRenderDispatcher;
    public IUiRenderBackend UiRenderBackend => uiRenderBackend;
    public Action PlayClickSound => playClickSound;
    public VirtualCursor VirtualCursor => virtualCursor;
    public Timer Timer => timer;
    public IScreenNavigator Navigator => navigator;
    public bool HasWorld => hasWorld();

    public int DisplayWidth => displaySize().X;
    public int DisplayHeight => displaySize().Y;

    public Func<Vector2D<int>> DisplaySize => displaySize;

    /// <summary>Pixel offset to subtract from raw mouse event coordinates.</summary>
    public Vector2D<int> MouseOffset => mouseOffset?.Invoke() ?? Vector2D<int>.Zero;

    /// <summary>
    /// The display dimensions used for input scaling. Normally equals <see cref="DisplayWidth"/>/<see cref="DisplayHeight"/>,
    /// but in debug viewport mode returns the viewport size so that click coordinates match the render coordinate space.
    /// </summary>
    public Vector2D<int> InputDisplaySize => inputDisplaySize?.Invoke() ?? displaySize();

    public IControllerState ControllerState => controllerState;
}
