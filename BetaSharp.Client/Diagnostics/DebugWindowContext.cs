using BetaSharp.Client.Entities;
using BetaSharp.Client.Rendering;
using BetaSharp.Util.Hit;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Diagnostics;

/// <summary>
/// Aggregates the inputs required by the debug window system so that individual windows
/// are not coupled directly to <see cref="BetaSharp"/>.
/// </summary>
internal sealed class DebugWindowContext
{
    private readonly BetaSharp _game;

    public World? World => _game.World;
    public ClientPlayerEntity? Player => _game.Player;
    public HitResult ObjectMouseOver => _game.ObjectMouseOver;
    public WorldRenderer? WorldRenderer => _game.WorldRenderer;
    public ParticleManager ParticleManager => _game.ParticleManager;
    public DebugSystemSnapshot DebugSystemSnapshot => _game.DebugSystemSnapshot;

    public DebugWindowContext(BetaSharp game) => _game = game;
}
