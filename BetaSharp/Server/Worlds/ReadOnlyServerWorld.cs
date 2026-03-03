using BetaSharp.Worlds;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Server.Worlds;

internal class ReadOnlyServerWorld : ServerWorld
{
    public ReadOnlyServerWorld(BetaSharpServer server, IWorldStorage storage, string saveName, int dimension, long seed, ServerWorld del) : base(server, storage, saveName, dimension, seed)
    {
        persistentStateManager = del.persistentStateManager;
        Properties = new DerivingWorldProperties(del.getProperties());
        Rules = del.Rules;
    }
}
