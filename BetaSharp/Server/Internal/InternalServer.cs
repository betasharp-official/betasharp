using BetaSharp.Server.Network;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Internal;

public class InternalServer : MinecraftServer
{
    private readonly string _worldPath;
    private readonly Lock _difficultyLock = new();
    private readonly int _initialDifficulty;
    private readonly ILogger<InternalServer> _logger = Log.Instance.For<InternalServer>();

    private int _lastDifficulty;

    public InternalServer(string worldPath, string levelName, string seed, int viewDistance, int initialDifficulty) : base(new InternalServerConfiguration(levelName, seed, viewDistance))
    {
        _worldPath = worldPath;
        LogHelp = false;
        _initialDifficulty = initialDifficulty;
        _lastDifficulty = _initialDifficulty;
    }

    public void SetViewDistance(int viewDistanceChunks)
    {
        InternalServerConfiguration serverConfiguration = (InternalServerConfiguration)Config;
        serverConfiguration.SetViewDistance(viewDistanceChunks);
        PlayerManager?.SetViewDistance(viewDistanceChunks);
    }

    public volatile bool IsReady;

    protected override bool Init()
    {
        Connections = new ConnectionListener(this);

        _logger.LogInformation("Starting internal server");

        bool result = base.Init();

        if (result)
        {
            for (int i = 0; i < Worlds.Length; ++i)
            {
                if (Worlds[i] != null)
                {
                    Worlds[i].difficulty = _initialDifficulty;
                    Worlds[i].allowSpawning(_initialDifficulty > 0, true);
                }
            }

            IsReady = true;
        }
        return result;
    }

    public override string GetFilePath(string path)
    {
        return Path.Combine(_worldPath, path);
    }

    public void SetDifficulty(int difficulty)
    {
        lock (_difficultyLock)
        {
            if (_lastDifficulty != difficulty)
            {
                _lastDifficulty = difficulty;
                for (int i = 0; i < Worlds.Length; ++i)
                {
                    if (Worlds[i] != null)
                    {
                        Worlds[i].difficulty = difficulty;
                        Worlds[i].allowSpawning(difficulty > 0, true);
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

                PlayerManager?.SendToAll(new BetaSharp.Network.Packets.Play.ChatMessagePacket($"Difficulty set to {difficultyName}"));
            }
        }
    }
}
