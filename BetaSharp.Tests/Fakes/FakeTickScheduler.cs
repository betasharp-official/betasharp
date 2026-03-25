using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Tests.Fakes;

public class FakeTickScheduler : WorldTickScheduler
{
    public FakeTickScheduler(IWorldContext context) : base(context)
    {
    }

    public override void ScheduleBlockUpdate(int x, int y, int z, int blockId, int tickRate, bool instantBlockUpdateEnabled = false)
    {
        // NO-OP for Fake World tests that ignore scheduling
    }
}
