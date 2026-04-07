using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class BanCommand : Command.Command
{
    public string Usage => "ban <player>";
    public string Description => "Bans a player";
    public string[] Names => ["ban"];
    public bool DisallowInternalServer => true;

    public void Execute(Command.Command.CommandSource c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: ban <player>");
            return;
        }

        string target = c.Args[0];
        c.Server.playerManager.banPlayer(target);
        c.LogOp("Banning " + target);
        c.Server.playerManager.getPlayer(target)?.NetworkHandler.disconnect("Banned by admin");
    }
}
