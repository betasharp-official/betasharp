using BetaSharp.Entities;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class TeleportDimensionCommand : ICommand
{
    public string Usage => "tpdim <id> [player]";
    public string Description => "Teleports to a dimension";
    public string[] Names => ["tpdim"];

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: /tpdim <dimension id> [player]");
            return;
        }

        if (!int.TryParse(c.Args[0], out int dim))
        {
            c.Output.SendMessage("Invalid dimension ID.");
            return;
        }

        if (dim != 0 && dim != -1)
        {
            c.Output.SendMessage("Dimension " + dim + " does not exist.");
            return;
        }

        ServerPlayerEntity? targetPlayer;
        if (c.Args.Length >= 2)
        {
            targetPlayer = c.Server.playerManager.getPlayer(c.Args[1]);
            if (targetPlayer == null)
            {
                c.Output.SendMessage("Player " + c.Args[1] + " not found.");
                return;
            }
        }
        else
        {
            targetPlayer = c.Server.playerManager.getPlayer(c.SenderName);
            if (targetPlayer == null)
            {
                c.Output.SendMessage("Could not find your player.");
                return;
            }
        }

        if (targetPlayer.dimensionId == dim)
        {
            c.Output.SendMessage("Player is already in dimension " + dim);
            return;
        }

        c.Server.playerManager.sendPlayerToDimension(targetPlayer, dim);
        c.Output.SendMessage("Teleported " + targetPlayer.name + " to dimension " + dim);
    }
}
