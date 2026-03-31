namespace BetaSharp.Client.Debug;

public class DebugOverlay(BetaSharp game)
{
    public readonly DebugContext Context = new(game);
    public List<DebugComponent> Components = [];
}
