using BetaSharp.Entities;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class KickCommand : ICommand
{
    public string Usage => "kick <player>";
    public string Description => "Kicks a player";
    public string[] Names => ["kick"];
    public bool DisallowInternalServer => true;

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: kick <player>");
            return;
        }

        string target = c.Args[0];
        ServerPlayerEntity? targetPlayer = c.Server.playerManager.getPlayer(target);

        if (targetPlayer != null)
        {
            targetPlayer.networkHandler.disconnect("Kicked by admin");
            c.LogOp("Kicking " + targetPlayer.name);
        }
        else
        {
            c.Output.SendMessage("Can't find user " + target + ". No kick.");
        }
    }
}
