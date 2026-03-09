using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;

namespace BetaSharp.Worlds.Core;

public interface IBlockWorldContext
{
    WorldBlockView BlocksReader { get; }
    WorldBlockWrite BlockWriter { get; }
    BlockHost BlockHost { get; }
    WorldEventBroadcaster Broadcaster { get; }
    RedstoneEngine Redstone { get; }
    EntityManager Entities { get; }
    LightingEngine Lighting { get; }
    EnvironmentManager Environment { get; }
    Dimension dimension { get; }

    bool IsRemote { get; }
    RuleSet Rules { get; }
    JavaRandom random { get; }
    long GetTime();
    void SpawnEntity(Entity entity);
    void SpawnItemDrop(double x, double y, double z, ItemStack itemStack);
}