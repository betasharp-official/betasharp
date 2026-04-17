using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public readonly record struct OnTickEvent(IWorldContext World, int X, int Y, int Z, int Meta, int BlockId);

public readonly record struct OnPlacedEvent(IWorldContext World, EntityLiving? Placer, Side Direction, Side Side, int X, int Y, int Z);

public readonly record struct CanPlaceAtContext(IWorldContext World, Side Direction, int X, int Y, int Z);

public readonly record struct OnUseEvent(IWorldContext World, EntityPlayer Player, int X, int Y, int Z);

public readonly record struct OnBreakEvent(IWorldContext World, Entity? Entity, int X, int Y, int Z, int Meta = 0);

public readonly record struct OnBlockBreakStartEvent(IWorldContext World, EntityPlayer Player, int X, int Y, int Z);

public readonly record struct OnDropEvent(IWorldContext World, int X, int Y, int Z, int Meta, float Luck = 1.0F);

public readonly record struct OnMetadataChangeEvent(IWorldContext World, int X, int Y, int Z, int Meta);

public readonly record struct OnEntityStepEvent(IWorldContext World, Entity Entity, int X, int Y, int Z);

public readonly record struct OnEntityCollisionEvent(IWorldContext World, Entity Entity, int X, int Y, int Z);

public readonly record struct OnApplyVelocityEvent(IWorldContext World, Entity Entity, int X, int Y, int Z);

public readonly record struct OnDestroyedByExplosionEvent(IWorldContext World, int X, int Y, int Z);

public readonly record struct OnAfterBreakEvent(IWorldContext World, EntityPlayer Player, int Meta, int X, int Y, int Z);

public readonly record struct OnBlockActionEvent(IWorldContext World, int Data1, int Data2, int X, int Y, int Z);
