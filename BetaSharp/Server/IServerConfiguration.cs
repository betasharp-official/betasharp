namespace BetaSharp.Server;

public interface IServerConfiguration
{
    string GetServerIp(string fallback);
    int GetServerPort(int fallback);
    bool GetDualStack(bool fallback);
    bool GetOnlineMode(bool fallback);
    bool GetSpawnAnimals(bool fallback);
    bool GetPvpEnabled(bool fallback);
    bool GetAllowFlight(bool fallback);
    string GetLevelName(string fallback);
    string GetLevelSeed(string fallback);
    bool GetSpawnMonsters(bool fallback);
    bool GetAllowNether(bool fallback);
    int GetMaxPlayers(int fallback);
    int GetViewDistance(int fallback);
    bool GetWhiteList(bool fallback);
    int GetSpawnRegionSize(int fallback);
    void Save();

    bool GetProperty(string property, bool fallback);
    int GetProperty(string property, int fallback);
    string GetProperty(string property, string fallback);
    void SetProperty(string property, bool value);
}
