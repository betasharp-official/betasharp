using BetaSharp.Entities;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class KillSelfCommand : ICommand
{
    public string Usage => "kill";
    public string Description => "Kills yourself";
    public string[] Names => ["kill"];

    public void Execute(ICommand.CommandContext c)
    {
        ServerPlayerEntity? player = c.Server.playerManager.getPlayer(c.SenderName);
        if (player == null)
        {
            c.Output.SendMessage("Could not find your player.");
            return;
        }

        player.damage(null, 1000);
    }
}
