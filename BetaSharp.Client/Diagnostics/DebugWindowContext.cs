using BetaSharp.Client.Entities;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Screens.InGame;
using BetaSharp.Util.Hit;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Diagnostics;

/// <summary>
/// Aggregates the inputs required by the debug window system so that individual windows
/// are not coupled directly to <see cref="BetaSharp"/>.
/// </summary>
internal sealed class DebugWindowContext(BetaSharp game)
{
    public World? World => game.World;
    public ClientPlayerEntity? Player => game.Player;
    public HitResult ObjectMouseOver => game.ObjectMouseOver;
    public WorldRenderer? WorldRenderer => game.WorldRenderer;
    public ParticleManager ParticleManager => game.ParticleManager;
    public DebugSystemSnapshot DebugSystemSnapshot => game.DebugSystemSnapshot;
    public UIScreen? CurrentScreen => game.CurrentScreen;
    public HUD HUD => game.HUD;
    public UIContext UIContext => game.UIContext;
}
