using BetaSharp.Entities;
using BetaSharp.Server.Worlds;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Storage.RegionFormat;

namespace BetaSharp.Worlds.Storage;

public interface IWorldStorage
{
    WorldProperties? LoadProperties();

    void CheckSessionLock();

    IChunkStorage? GetChunkStorage(Dimension dimension);

    void Save(WorldProperties properties, List<EntityPlayer> players);

    void Save(WorldProperties properties);

    void ForceSave();

    IPlayerStorage? GetPlayerStorage();

    FileInfo? GetWorldPropertiesFile(string name);
}
