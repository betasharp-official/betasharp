using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp;

public record SpawnListEntry(Func<IWorldContext, EntityLiving> Factory);
