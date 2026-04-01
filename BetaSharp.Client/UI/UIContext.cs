using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Screens;
using Silk.NET.Maths;

namespace BetaSharp.Client.UI;

public sealed class UIContext(
    GameOptions options,
    TextRenderer textRenderer,
    TextureManager textureManager,
    Action playClickSound,
    Func<Vector2D<int>> displaySize,
    IControllerState controllerState,
    VirtualCursor virtualCursor,
    Timer timer,
    IScreenNavigator navigator,
    Func<bool> hasWorld)
{

    public GameOptions Options => options;
    public TextRenderer TextRenderer => textRenderer;
    public TextureManager TextureManager => textureManager;
    public Action PlayClickSound => playClickSound;
    public VirtualCursor VirtualCursor => virtualCursor;
    public Timer Timer => timer;
    public IScreenNavigator Navigator => navigator;
    public bool HasWorld => hasWorld();

    public int DisplayWidth => displaySize().X;
    public int DisplayHeight => displaySize().Y;

    public Func<Vector2D<int>> DisplaySize => displaySize;

    public IControllerState ControllerState => controllerState;
}
