using java.io;
using java.lang;
using java.util;
using java.util.logging;
using Exception = System.Exception;

namespace BetaSharp.Server;

internal class DedicatedServerConfiguration : IServerConfiguration
{
    public static Logger logger = Logger.getLogger("Minecraft");
    private readonly Properties properties = new();
    private readonly java.io.File propertiesFile;

    public DedicatedServerConfiguration(java.io.File file)
    {
        propertiesFile = file;
        if (file.exists())
        {
            try
            {
                properties.load(new FileInputStream(file));
            }
            catch (Exception ex)
            {
                logger.log(Level.WARNING, "Failed to load " + file, (Throwable)ex);
                generateNew();
            }
        }
        else
        {
            logger.log(Level.WARNING, file + " does not exist");
            generateNew();
        }
    }

    public void generateNew()
    {
        logger.log(Level.INFO, "Generating new properties file");
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
            properties.store(new FileOutputStream(propertiesFile), "Minecraft server properties");
        }
        catch (Exception ex)
        {
            logger.log(Level.WARNING, "Failed to save " + propertiesFile, ex);
            generateNew();
        }
    }

    public string getProperty(string property, string fallback)
    {
        return GetProperty(property, fallback);
    }

    public string GetProperty(string property, string fallback)
    {
        if (!properties.containsKey(property))
        {
            properties.setProperty(property, fallback);
            save();
        }

        return properties.getProperty(property, fallback);
    }

    public int getProperty(string property, int fallback)
    {
        return GetProperty(property, fallback);
    }

    public int GetProperty(string property, int fallback)
    {
        try
        {
            return Integer.parseInt(getProperty(property, "" + fallback));
        }
        catch (Exception)
        {
            properties.setProperty(property, "" + fallback);
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
            return java.lang.Boolean.parseBoolean(getProperty(property, "" + fallback));
        }
        catch (Exception)
        {
            properties.setProperty(property, "" + fallback);
            return fallback;
        }
    }

    public void setProperty(string property, bool value)
    {
        SetProperty(property, value);
    }

    public void SetProperty(string property, bool value)
    {
        properties.setProperty(property, "" + value);
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
    public string GetLevelSeed(string fallback) => GetProperty("level-seed", fallback);
    public bool GetSpawnMonsters(bool fallback) => GetProperty("spawn-monsters", fallback);
    public bool GetAllowNether(bool fallback) => GetProperty("allow-nether", fallback);
    public int GetMaxPlayers(int fallback) => GetProperty("max-players", fallback);
    public int GetViewDistance(int fallback) => GetProperty("view-distance", fallback);
    public bool GetWhiteList(bool fallback) => GetProperty("white-list", fallback);
    public int GetSpawnRegionSize(int fallback) => GetProperty("spawn-region-size", fallback);
}
