using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace BetaSharp.Server;

internal class DedicatedServerConfiguration : IServerConfiguration
{
    private static ILogger<DedicatedServerConfiguration> logger = Log.Instance.For<DedicatedServerConfiguration>();
    private readonly  Properties _properties = new();
    private readonly FileInfo _propertiesFile;

    public DedicatedServerConfiguration(FileInfo file)
    {
        _propertiesFile = file;
        if (file.Exists)
        {
            try
            {
                _properties = Properties.Load(file.FullName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load " + file);
                generateNew();
            }
        }
        else
        {
            logger.LogWarning(file + " does not exist");
            generateNew();
        }
    }

    public void generateNew()
    {
        logger.LogInformation("Generating new properties file");
        save();
    }

    public void save()
    {
        Save();
    }

    public void Save()
    {
        try
        {
            _properties.Save(_propertiesFile.FullName, "BetaSharp server properties");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save " + _propertiesFile);
            generateNew();
        }
    }

    public string getProperty(string property, string fallback)
    {
        return GetProperty(property, fallback);
    }

    public string GetProperty(string property, string fallback)
    {
        if (_properties.TryGetValue(property, out string? propertyValue))
        {
            return propertyValue;
        }

        _properties.SetProperty(property, fallback);
        save();

        return propertyValue ?? fallback;
    }

    public int getProperty(string property, int fallback)
    {
        return GetProperty(property, fallback);
    }

    public int GetProperty(string property, int fallback)
    {
        try
        {
            return int.Parse(getProperty(property, "" + fallback));
        }
        catch (Exception)
        {
            _properties.SetProperty(property, "" + fallback);
            return fallback;
        }
    }

    public bool getProperty(string property, bool fallback)
    {
        return GetProperty(property, fallback);
    }

    public bool GetProperty(string property, bool fallback)
    {
        try
        {
            return bool.Parse(getProperty(property, "" + fallback));
        }
        catch (Exception)
        {
            _properties.SetProperty(property, "" + fallback);
            return fallback;
        }
    }

    public void setProperty(string property, bool value)
    {
        SetProperty(property, value);
    }

    public void SetProperty(string property, bool value)
    {
        _properties.SetProperty(property, "" + value);
        save();
    }

    public string GetServerIp(string fallback) => GetProperty("server-ip", fallback);
    public int GetServerPort(int fallback) => GetProperty("server-port", fallback);
    public bool GetDualStack(bool fallback) => GetProperty("dual-stack", fallback);
    public bool GetOnlineMode(bool fallback) => GetProperty("online-mode", fallback);
    public bool GetSpawnAnimals(bool fallback) => GetProperty("spawn-animals", fallback);
    public bool GetPvpEnabled(bool fallback) => GetProperty("pvp", fallback);
    public bool GetAllowFlight(bool fallback) => GetProperty("allow-flight", fallback);
    public string GetLevelName(string fallback) => GetProperty("level-name", fallback);
    public string GetLevelType(string fallback) => GetProperty("level-type", fallback);
    public string GetLevelSeed(string fallback) => GetProperty("level-seed", fallback);
    public string GetLevelOptions(string fallback) => GetProperty("generator-settings", fallback);
    public bool GetSpawnMonsters(bool fallback) => GetProperty("spawn-monsters", fallback);
    public bool GetAllowNether(bool fallback) => GetProperty("allow-nether", fallback);
    public int GetMaxPlayers(int fallback) => GetProperty("max-players", fallback);
    public int GetViewDistance(int fallback) => GetProperty("view-distance", fallback);
    public bool GetWhiteList(bool fallback) => GetProperty("white-list", fallback);
    public int GetSpawnRegionSize(int fallback) => GetProperty("spawn-region-size", fallback);
    public int GetDefaultGamemode(int fallback) => GetProperty("default-gamemode", fallback);
}
