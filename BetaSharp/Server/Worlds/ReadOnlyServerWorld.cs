using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Server.Worlds;

internal class ReadOnlyServerWorld : ServerWorld
{
    public ReadOnlyServerWorld(BetaSharpServer server, IWorldStorage storage, string saveName, int dimension, long seed, ServerWorld del) : base(server, storage, saveName, dimension, seed)
    {
        PersistentStateManager = del.PersistentStateManager;
        Properties = new DerivingWorldProperties(del.Properties);
        Rules = del.Rules;
    }
}
