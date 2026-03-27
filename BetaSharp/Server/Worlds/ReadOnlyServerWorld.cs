using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Server.Worlds;

internal class ReadOnlyServerWorld : ServerWorld
{
    public ReadOnlyServerWorld(BetaSharpServer server, IWorldStorage storage, string saveName, int dimension, WorldSettings settings, ServerWorld del) : base(server, storage, saveName, dimension, settings, del)
    {
        StateManager = del.StateManager;
        Properties = new DerivingWorldProperties(del.Properties);
        Rules = del.Rules;
    }
}
