using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal class DedicatedPlayerManager : PlayerManager
{
    private const string BannedPlayersPath = "banned-players.txt";
    private const string BannedIpsPath = "banned-ips.txt";
    private const string OperatorsPath = "ops.txt";
    private const string WhitelistPath = "white-list.txt";

    private readonly ILogger<DedicatedPlayerManager> _logger = Log.Instance.For<DedicatedPlayerManager>();

    public DedicatedPlayerManager(BetaSharpServer server) : base(server)
    {
        loadBannedPlayers();
        loadBannedIps();
        loadOperators();
        loadWhitelist();
        saveBannedPlayers();
        saveBannedIps();
        saveOperators();
        saveWhitelist();
    }

    protected override void loadBannedPlayers()
    {
        try
        {
            bannedPlayers.Clear();
            using StreamReader reader = new(BannedPlayersPath);

            while (reader.ReadLine() is { } line)
            {
                bannedPlayers.Add(line.Trim().ToLower());
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to load ban list {Exception}", exception);
        }
    }

    protected override void saveBannedPlayers()
    {
        try
        {
            using StreamWriter writer = new(BannedPlayersPath);

            foreach (string player in bannedPlayers)
            {
                writer.WriteLine(player);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to save ban list {Exception}", exception);
        }
    }

    protected override void loadBannedIps()
    {
        try
        {
            bannedIps.Clear();
            using StreamReader reader = new(BannedIpsPath);

            while (reader.ReadLine() is { } line)
            {
                bannedIps.Add(line.Trim().ToLower());
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to load IP ban list {Exception}", exception);
        }
    }

    protected override void saveBannedIps()
    {
        try
        {
            using StreamWriter writer = new(BannedIpsPath);

            foreach (string ip in bannedIps)
            {
                writer.WriteLine(ip);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to save IP ban list {Exception}", exception);
        }
    }

    protected override void loadOperators()
    {
        try
        {
            ops.Clear();
            using StreamReader reader = new(OperatorsPath);

            while (reader.ReadLine() is { } line)
            {
                ops.Add(line.Trim().ToLower());
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to load OP list {Exception}", exception);
        }
    }

    protected override void saveOperators()
    {
        try
        {
            using StreamWriter writer = new(OperatorsPath);

            foreach (string op in ops)
            {
                writer.WriteLine(op);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to save OP list {Exception}", exception);
        }
    }

    protected override void loadWhitelist()
    {
        try
        {
            whitelist.Clear();
            using StreamReader reader = new(WhitelistPath);

            while (reader.ReadLine() is { } line)
            {
                whitelist.Add(line.Trim().ToLower());
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to load white-lis: {Exception}", exception);
        }
    }

    protected override void saveWhitelist()
    {
        try
        {
            using StreamWriter writer = new(WhitelistPath);

            foreach (string name in whitelist)
            {
                writer.WriteLine(name);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to save white-list {Exception}", exception);
        }
    }
}
