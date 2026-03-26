namespace BetaSharp;

internal record struct BlockUpdate(int X, int Y, int Z, int BlockId, long ScheduledTime)
{
    private static long NextOrder = 0;
    public readonly long ScheduledOrder = Interlocked.Increment(ref NextOrder);
}
