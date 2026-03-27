using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.PathFinding;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Worlds.Core.Systems;

public interface IWorldContext
{
    public IBlockReader Reader { get; }
    public IBlockWriter Writer { get; }
    public ChunkHost ChunkHost { get; }
    public WorldEventBroadcaster Broadcaster { get; }
    public RedstoneEngine Redstone { get; }
    public EntityManager Entities { get; }
    public LightingEngine Lighting { get; }
    public EnvironmentManager Environment { get; }
    public Dimension Dimension { get; }
    public WorldTickScheduler TickScheduler { get; }
    public long Seed { get; }
    public bool IsRemote { get; }
    public RuleSet Rules { get; }
    public PersistentStateManager StateManager { get; }
    public int Difficulty { get; }
    public WorldProperties Properties { get; }
    public JavaRandom Random { get; }
    internal PathFinder Pathing { get; }
    public void SetDifficulty(int difficulty);
    public long GetTime();
    public int GetSpawnBlockId(int x, int z);
    public bool SpawnEntity(Entity entity);
    public bool SpawnItemDrop(double x, double y, double z, ItemStack itemStack);
    public bool CanInteract(EntityPlayer player, int x, int y, int z);
    public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power, bool fire);
    public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power);
}
