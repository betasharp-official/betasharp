using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class OpCommand : ICommand
{
    public string Usage => "op <player>";
    public string Description => "Makes a player operator";
    public string[] Names => ["op"];
    public bool DisallowInternalServer => true;

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: op <player>");
            return;
        }

        string target = c.Args[0];
        c.Server.playerManager.addToOperators(target);
        c.LogOp("Opping " + target);
        c.Server.playerManager.messagePlayer(target, "§eYou are now op!");
    }
}
