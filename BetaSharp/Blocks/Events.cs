using BetaSharp.Entities;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Dimensions;

namespace BetaSharp.Blocks;

public struct OnTickEvt(
    WorldBlockView worldRead,
    WorldBlockWrite worldWrite,
    WorldEventBroadcaster broadcaster,
    RedstoneEngine redstone,
    EntityManager entities,
    RuleSet rules,
    EnvironmentManager environment,
    Dimension dimension,
    LightingEngine lighting,
    JavaRandom random,
    bool isRemote,
    long time,
    int x,
    int y,
    int z,
    int meta,
    int blockId)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public RedstoneEngine Redstone = redstone;
    public EntityManager Entities = entities;
    public RuleSet Rules = rules;
    public EnvironmentManager Environment = environment;
    public Dimension Dimension = dimension;
    public LightingEngine Lighting = lighting;
    public JavaRandom Random = random;
    public bool IsRemote = isRemote;
    public long Time = time;
    public int X = x;
    public int Y = y;
    public int Z = z;
    public int Meta = meta;
    public int BlockId = blockId;
}

public struct OnPlacedEvt(WorldBlockView worldRead, WorldBlockWrite worldWrite, RedstoneEngine redstone, EntityLiving placer, WorldEventBroadcaster broadcaster, bool isRemote, int direction, int side, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public RedstoneEngine Redstone = redstone;
    public EntityLiving Placer = placer;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public bool IsRemote = isRemote;
    public int Direction = direction;
    public int Side = side;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct CanPlaceAtCtx(IBlockReader worldRead, IBlockWrite worldWrite, int x, int y, int z)
{
    public IBlockReader WorldRead = worldRead;
    public IBlockWrite WorldWrite = worldWrite;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnUseEvt(WorldBlockView worldRead, WorldBlockWrite worldWrite, WorldEventBroadcaster broadcaster, Dimension dimension, EntityManager entities, EntityPlayer player, bool isRemote, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public Dimension Dimension = dimension;
    public EntityManager Entities = entities;
    public EntityPlayer Player = player;
    public bool IsRemote = isRemote;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnBreakEvt(WorldBlockView worldRead, WorldBlockWrite worldWrite, WorldEventBroadcaster broadcaster, Dimension dimension, EntityManager entities, Entity entity, bool isRemote, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public Dimension Dimension = dimension;
    public EntityManager Entities = entities;
    public Entity Entity = entity;
    public bool IsRemote = isRemote;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnBlockBreakStartEvt(IBlockReader worldRead, IBlockWrite worldWrite, WorldEventBroadcaster broadcaster, int x, int y, int z)
{
    public IBlockReader WorldRead = worldRead;
    public IBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnDropEvt(IBlockReader worldRead, IBlockWorldContext context, RuleSet rules, bool isRemote, int x, int y, int z, int meta, float luck = 1.0F)
{
    public IBlockReader WorldRead = worldRead;
    public IBlockWorldContext Context = context;
    public RuleSet Rules = rules;
    public bool IsRemote = isRemote;
    public int X = x;
    public int Y = y;
    public int Z = z;
    public int Meta = meta;
    public float Luck = luck;
}

public struct OnMetadataChangeEvt(IBlockReader read, IBlockWrite write, int x, int y, int z, int meta)
{
    public IBlockReader WorldRead = read;
    public IBlockWrite WorldWrite = write;
    public int X = x;
    public int Y = y;
    public int Z = z;
    public int Meta = meta;
}

public struct OnEntityStepEvt(WorldBlockView worldRead, WorldBlockWrite worldWrite, WorldEventBroadcaster broadcaster, EntityManager entities, Entity entity, bool isRemote, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public EntityManager Entities = entities;
    public Entity Entity = entity;
    public bool IsRemote = isRemote;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnEntityCollisionEvt(WorldBlockView worldRead, WorldBlockWrite worldWrite, WorldEventBroadcaster broadcaster, EntityManager entities, Entity entity, bool isRemote, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public EntityManager Entities = entities;
    public Entity Entity = entity;
    public bool IsRemote = isRemote;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnApplyVelocityEvt(WorldBlockView worldRead, Entity entity, Vec3D velocity, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public Entity Entity = entity;
    public Vec3D Velocity = velocity;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnDestroyedByExplosionEvt(WorldBlockView worldRead, WorldBlockWrite worldWrite, WorldEventBroadcaster broadcaster, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnAfterBreakEvt(WorldBlockView worldRead, WorldBlockWrite worldWrite, WorldEventBroadcaster broadcaster, RuleSet rules, JavaRandom random, EntityPlayer player, int meta, bool isRemote, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public RuleSet Rules = rules;
    public JavaRandom Random = random;
    public EntityPlayer Player = player;
    public int Meta = meta;
    public bool IsRemote = isRemote;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnBlockActionEvt(WorldBlockView worldRead, WorldBlockWrite worldWrite, WorldEventBroadcaster broadcaster, int data1, int data2, int x, int y, int z)
{
    public WorldBlockView WorldRead = worldRead;
    public WorldBlockWrite WorldWrite = worldWrite;
    public WorldEventBroadcaster Broadcaster = broadcaster;
    public int Data1 = data1;
    public int Data2 = data2;
    public int X = x;
    public int Y = y;
    public int Z = z;
}
