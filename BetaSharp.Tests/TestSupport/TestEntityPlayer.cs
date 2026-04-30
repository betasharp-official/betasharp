using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Tests.TestSupport;

/// <summary>Minimal concrete player for tests that need a real <see cref="EntityPlayer"/> in the world (AI, interaction).</summary>
public sealed class TestEntityPlayer : EntityPlayer
{
    public TestEntityPlayer(IWorldContext world) : base(world)
    {
    }

    public override EntityType Type => EntityRegistry.Player;

    public override void Spawn()
    {
    }
}
