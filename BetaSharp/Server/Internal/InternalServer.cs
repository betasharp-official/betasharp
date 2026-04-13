using BetaSharp.Server.Network;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Internal;

public class InternalServer : BetaSharpServer
{
    private readonly string _worldPath;
    private readonly Lock _difficultyLock = new();
    private readonly int _initialDifficulty;
    private readonly ILogger<InternalServer> _logger = Log.Instance.For<InternalServer>();

    private int _lastDifficulty;

    public InternalServer(string worldPath, string levelName, WorldSettings settings, int viewDistance, int initialDifficulty) :
        base(new InternalServerConfiguration(levelName, settings.TerrainType.Name, settings.Seed.ToString(), settings.GeneratorOptions, viewDistance))
    {
        _worldPath = worldPath;
        logHelp = false;
        _initialDifficulty = initialDifficulty;
        _lastDifficulty = _initialDifficulty;
    }

    public void SetViewDistance(int viewDistanceChunks)
    {
        InternalServerConfiguration serverConfiguration = (InternalServerConfiguration)config;
        serverConfiguration.SetViewDistance(viewDistanceChunks);
        playerManager?.SetViewDistance(viewDistanceChunks);
    }

    public volatile bool isReady;

    protected override bool Init()
    {
        connections = new ConnectionListener(this);

        _logger.LogInformation("Starting internal server");

        bool result = base.Init();

        if (result)
        {
            for (int i = 0; i < worlds.Length; ++i)
            {
                if (worlds[i] != null)
                {
                    worlds[i].SetDifficulty(_initialDifficulty);
                    worlds[i].allowSpawning(_initialDifficulty > 0, true);
                }
            }

            isReady = true;
        }
        return result;
    }

    public override FileInfo GetFile(string path)
    {
        return new(Path.Combine(_worldPath, path));
    }

    public void SetDifficulty(int difficulty)
    {
        lock (_difficultyLock)
        {
            if (_lastDifficulty != difficulty)
            {
                _lastDifficulty = difficulty;
                for (int i = 0; i < worlds.Length; ++i)
                {
                    if (worlds[i] != null)
                    {
                        worlds[i].SetDifficulty(difficulty);
                        worlds[i].allowSpawning(difficulty > 0, true);
                    }
                }

                string difficultyName = difficulty switch
                {
                    0 => "Peaceful",
                    1 => "Easy",
                    2 => "Normal",
                    3 => "Hard",
                    _ => "Unknown"
                };

                playerManager?.sendToAll(BetaSharp.Network.Packets.Play.ChatMessagePacket.Get($"Difficulty set to {difficultyName}"));
            }
        }
    }
}
