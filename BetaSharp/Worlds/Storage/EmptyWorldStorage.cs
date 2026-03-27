using BetaSharp.Entities;
using BetaSharp.Server.Worlds;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Storage.RegionFormat;

namespace BetaSharp.Worlds.Storage;

public class EmptyWorldStorage : IWorldStorage
{
    public WorldProperties? LoadProperties() => null;

    public void CheckSessionLock()
    {
    }

    public IChunkStorage? GetChunkStorage(Dimension dimension) => null;

    public void Save(WorldProperties properties, List<EntityPlayer> players)
    {
    }

    public void Save(WorldProperties properties)
    {
    }

    public FileInfo? GetWorldPropertiesFile(string name) => null;

    public void ForceSave()
    {
    }

    public IPlayerStorage? GetPlayerStorage() => throw new NotImplementedException();
}
