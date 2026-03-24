using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal class DedicatedPlayerManager : PlayerManager
{
    private readonly ILogger<DedicatedPlayerManager> _logger = Log.Instance.For<DedicatedPlayerManager>();
    private readonly FileInfo _bannedPlayersFile;
    private readonly FileInfo _bannedIpsFile;
    private readonly FileInfo _operatorsFile;
    private readonly FileInfo _whitelistFile;

    public DedicatedPlayerManager(BetaSharpServer server) : base(server)
    {
        _bannedPlayersFile = server.GetFile("banned-players.txt");
        _bannedIpsFile = server.GetFile("banned-ips.txt");
        _operatorsFile = server.GetFile("ops.txt");
        _whitelistFile = server.GetFile("white-list.txt");

        loadBannedPlayers();
        loadBannedIps();
        loadOperators();
        loadWhitelist();
        saveBannedPlayers();
        saveBannedIps();
        saveOperators();
        saveWhitelist();
    }

    protected sealed override void loadBannedPlayers() => loadFileInto(_bannedPlayersFile, bannedPlayers);

    protected sealed override void saveBannedPlayers() => saveToFile(bannedPlayers, _bannedPlayersFile);

    protected sealed override void loadBannedIps() => loadFileInto(_bannedIpsFile, bannedIps);

    protected sealed override void saveBannedIps() => saveToFile(bannedIps, _bannedIpsFile);

    protected sealed override void loadOperators() => loadFileInto(_operatorsFile, ops);

    protected sealed override void saveOperators() => saveToFile(ops, _operatorsFile);

    protected sealed override void loadWhitelist() => loadFileInto(_whitelistFile, whitelist);

    protected sealed override void saveWhitelist() => saveToFile(whitelist, _whitelistFile);

    private void saveToFile(HashSet<string> lines, FileInfo file)
    {
        try
        {
            StreamWriter writer = new(file.Open(FileMode.Create));

            foreach (string whitelistedPlayer in lines)
            {
                writer.WriteLine(whitelistedPlayer);
            }

            writer.Close();
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"Failed to save {file.Name}: {exception}");
        }
    }

    private void loadFileInto(FileInfo file, HashSet<string> fileContent)
    {
        try
        {
            fileContent.Clear();
            StreamReader reader = new (file.Open(FileMode.OpenOrCreate));

            while (reader.ReadLine() is { } entry)
            {
                fileContent.Add(entry.Trim().ToLower());
            }

            reader.Close();
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"Failed to read {file.Name}: {exception}");
        }
    }
}
