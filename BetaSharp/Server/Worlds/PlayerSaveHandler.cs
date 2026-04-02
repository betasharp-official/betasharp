using BetaSharp.Entities;

namespace BetaSharp.Server.Worlds;

public interface IPlayerStorage
{
    void SavePlayerData(EntityPlayer player);

    void LoadPlayerData(EntityPlayer player);
}
