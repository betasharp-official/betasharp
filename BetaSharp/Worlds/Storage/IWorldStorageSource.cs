using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Storage;

public interface IWorldStorageSource
{
    string Name { get; }

    IWorldStorage Get(string saveName, bool createPlayerDataDir);

    List<WorldSaveInfo> GetAll();

    void Flush();

    WorldProperties? GetProperties(string saveName);

    void Delete(string saveName);

    void Rename(string saveName, string newName);
}
