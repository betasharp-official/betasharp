using System.Net;
using BetaSharp.Recipes;
using BetaSharp.Registries;
using BetaSharp.Server.Network;
using BetaSharp.Server.Threading;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace BetaSharp.Server;

internal class DedicatedServer(IServerConfiguration config) : BetaSharpServer(config)
{
    private static readonly ILogger<DedicatedServer> s_logger = Log.Instance.For<DedicatedServer>();

    public override FileInfo GetFile(string path) => new(Path.Combine(".", path));

    protected override PlayerManager CreatePlayerManager()
    {
        return new DedicatedPlayerManager(this);
    }

    protected override bool Init()
    {
        new ConsoleInputThread(this).Run();

        s_logger.LogInformation("Starting BetaSharp server version Beta 1.7.3");

        long availableMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024L * 1024L);
        if (availableMb < 512)
        {
            s_logger.LogWarning("**** NOT ENOUGH RAM!");
            s_logger.LogWarning("To start the server ensure that a minimum of 512MB of RAM is available.");
        }

        RegistryAccess = RegistryAccess.Build(datapackPath: ".");

        s_logger.LogInformation("Loading properties");

        string addressInput = config.GetServerIp("");

        bool dualStack = config.GetDualStack(false);

        var address = dualStack ? IPAddress.IPv6Any : IPAddress.Any;

        if (addressInput.Length > 0)
        {
            address = Dns.GetHostAddresses(addressInput)[0];
        }

        int port = config.GetServerPort(25565);
        s_logger.LogInformation($"Starting BetaSharp server on {(addressInput.Length == 0 ? "*" : addressInput)}:{port}");

        try
        {
            connections = new ConnectionListener(this, address, port, dualStack);
        }
        catch (IOException ex)
        {
            s_logger.LogWarning("**** FAILED TO BIND TO PORT!");
            s_logger.LogWarning($"The exception was: {ex}");
            s_logger.LogWarning("Perhaps a server is already running on that port?");
            return false;
        }

        if (!onlineMode)
        {
            s_logger.LogWarning("**** SERVER IS RUNNING IN OFFLINE/INSECURE MODE!");
            s_logger.LogWarning("The server will make no attempt to authenticate usernames. Beware.");
            s_logger.LogWarning("While this makes the game possible to play without internet access, it also opens up the ability for hackers to connect with any username they choose.");
            s_logger.LogWarning("To change this, set \"online-mode\" to \"true\" in the server.settings file.");
        }

        return base.Init();
    }

    public static void Main()
    {
        Log.Instance.Initialize(Directory.GetCurrentDirectory());
        AssetManager.Initialize(AssetManager.AssetProfile.Headless);
        Bootstrap.Initialize();

        try
        {
            DedicatedServerConfiguration config = new(new FileInfo("server.properties"));
            DedicatedServer server = new(config);
            server.RunThreaded("Server Thread");
        }
        catch (Exception e)
        {
            s_logger.LogError($"Failed to start the BetaSharp server: {e}");
        }
    }
}
