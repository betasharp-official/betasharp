using BetaSharp.Util;

namespace BetaSharp.Client.Debug;

public class DebugContext(BetaSharp game)
{
    public readonly BetaSharp Game = game;
    public readonly GCMonitor GCMonitor = new();
}
