using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public readonly record struct OnTickEvt(IWorldContext Level, int X, int Y, int Z, int Meta, int BlockId);

public readonly record struct OnPlacedEvt(IWorldContext Level, EntityLiving? Placer, int Direction, int Side, int X, int Y, int Z);

public readonly record struct CanPlaceAtCtx(IWorldContext Level, int Direction, int X, int Y, int Z);

public readonly record struct OnUseEvt(IWorldContext Level, EntityPlayer Player, int X, int Y, int Z);

public readonly record struct OnBreakEvt(IWorldContext Level, Entity? Entity, int X, int Y, int Z);

public readonly record struct OnBlockBreakStartEvt(IWorldContext Level, EntityPlayer Player, int X, int Y, int Z);

public readonly record struct OnDropEvt(IWorldContext Level, int X, int Y, int Z, int Meta, float Luck = 1.0F);

public readonly record struct OnMetadataChangeEvt(IWorldContext Level, int X, int Y, int Z, int Meta);

public readonly record struct OnEntityStepEvt(IWorldContext Level, Entity Entity, int X, int Y, int Z);

public readonly record struct OnEntityCollisionEvt(IWorldContext Level, Entity Entity, int X, int Y, int Z);

public readonly record struct OnApplyVelocityEvt(IWorldContext Level, Entity Entity, int X, int Y, int Z);

public readonly record struct OnDestroyedByExplosionEvt(IWorldContext Level, int X, int Y, int Z);

public readonly record struct OnAfterBreakEvt(IWorldContext Level, EntityPlayer Player, int Meta, int X, int Y, int Z);

public readonly record struct OnBlockActionEvt(IWorldContext Level, int Data1, int Data2, int X, int Y, int Z);
