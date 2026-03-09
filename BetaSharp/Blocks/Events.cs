using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public struct OnTickEvt(IBlockWorldContext level, int x, int y, int z, int meta, int blockId)
{
    public IBlockWorldContext Level = level;
    public int X = x;
    public int Y = y;
    public int Z = z;
    public int Meta = meta;
    public int BlockId = blockId;
}

public struct OnPlacedEvt(IBlockWorldContext level, EntityLiving? placer, int direction, int side, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public EntityLiving? Placer = placer;
    public int Direction = direction;
    public int Side = side;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct CanPlaceAtCtx(IBlockWorldContext level, int direction, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public int Direction = direction;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnUseEvt(IBlockWorldContext level, EntityPlayer player, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public EntityPlayer Player = player;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnBreakEvt(IBlockWorldContext level, Entity? entity, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public Entity? Entity = entity;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnBlockBreakStartEvt(IBlockWorldContext level, EntityPlayer player, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public EntityPlayer Player = player;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnDropEvt(IBlockWorldContext level, int x, int y, int z, int meta, float luck = 1.0F)
{
    public IBlockWorldContext Level = level;
    public int X = x;
    public int Y = y;
    public int Z = z;
    public int Meta = meta;
    public float Luck = luck;
}

public struct OnMetadataChangeEvt(IBlockWorldContext level, int x, int y, int z, int meta)
{
    public IBlockWorldContext Level = level;
    public int X = x;
    public int Y = y;
    public int Z = z;
    public int Meta = meta;
}

public struct OnEntityStepEvt(IBlockWorldContext level, Entity entity, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public Entity Entity = entity;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnEntityCollisionEvt(IBlockWorldContext level, Entity entity, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public Entity Entity = entity;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnApplyVelocityEvt(IBlockWorldContext level, Entity entity, Vec3D velocity, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public Entity Entity = entity;
    public Vec3D Velocity = velocity;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnDestroyedByExplosionEvt(IBlockWorldContext level, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnAfterBreakEvt(IBlockWorldContext level, EntityPlayer player, int meta, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public EntityPlayer Player = player;
    public int Meta = meta;
    public int X = x;
    public int Y = y;
    public int Z = z;
}

public struct OnBlockActionEvt(IBlockWorldContext level, int data1, int data2, int x, int y, int z)
{
    public IBlockWorldContext Level = level;
    public int Data1 = data1;
    public int Data2 = data2;
    public int X = x;
    public int Y = y;
    public int Z = z;
}