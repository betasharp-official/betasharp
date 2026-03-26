using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class PardonCommand : ICommand
{
    public string Usage => "pardon <player>";
    public string Description => "Pardons a player";
    public string[] Names => ["pardon"];
    public bool DisallowInternalServer => true;

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: pardon <player>");
            return;
        }

        string target = c.Args[0];
        c.Server.playerManager.unbanPlayer(target);
        c.LogOp("Pardoning " + target);
    }
}
