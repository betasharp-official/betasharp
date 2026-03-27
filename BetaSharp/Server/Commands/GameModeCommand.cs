using BetaSharp.Entities;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server.Command;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Commands;

public class GameModeCommand : ICommand
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(GameModeCommand));

    // ReSharper disable once StringLiteralTypo
    public string Usage => "gamemode <player> <mode>";
    public string Description => "Broadcasts a message";

    // ReSharper disable once StringLiteralTypo
    public string[] Names => ["gamemode", "gm"];

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length == 0)
        {
            var p = c.Server.playerManager.getPlayer(c.SenderName)!;
            c.Output.SendMessage(p.GameMode.Name);
            return;
        }

        if (c.Args.Length == 1)
        {
            var p = c.Server.playerManager.getPlayer(c.SenderName)!;
            SetGameMode(p, c.Args[0], c);
        }
        else
        {
            var p = c.Server.playerManager.getPlayer(c.Args[1]);
            if (p == null)
            {
                c.Output.SendMessage("Player not found.");
                return;
            }

            SetGameMode(p, c.Args[1], c);
        }
    }

    private void SetGameMode(EntityPlayer p, string arg, ICommand.CommandContext c)
    {
        // mode by id
        if (int.TryParse(arg, out int mode))
        {
            if (GameModes.TryGet(mode, out var gameMode))
            {
                SetGameMode(p, gameMode, c);
            }
            else
            {
                c.Output.SendMessage("Mode not found.");
            }
        }
        // mode by letter
        else if (arg.Length == 1)
        {
            if (GameModes.TryGet(arg[0], out var gameMode))
            {
                SetGameMode(p, gameMode, c);
            }
            else
            {
                c.Output.SendMessage("Mode not found.");
            }
        }
        // mode by name
        else
        {
            if (GameModes.TryGet(arg, out var gameMode))
            {
                SetGameMode(p, gameMode, c);
            }
            else
            {
                c.Output.SendMessage("Mode not found.");
            }
        }
    }

    private void SetGameMode(EntityPlayer p, GameMode gameMode, ICommand.CommandContext c)
    {
        p.GameMode = gameMode;
        string s = $"{p.name} game mode set to {gameMode.Name}.";
        s_logger.LogInformation(s);
        c.Output.SendMessage(s);
    }
}
