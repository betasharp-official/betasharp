using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class DeopCommand : ICommand
{
    public string Usage => "deop <player>";
    public string Description => "Removes operator status";
    public string[] Names => ["deop"];
    public bool DisallowInternalServer => true;

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: deop <player>");
            return;
        }

        string target = c.Args[0];
        c.Server.playerManager.removeFromOperators(target);
        c.Server.playerManager.messagePlayer(target, "§eYou are no longer op!");
        c.LogOp("De-opping " + target);
    }
}
