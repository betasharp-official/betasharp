using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class ListCommand : Command.Command
{
    public string Usage => "list";
    public string Description => "Lists connected players";
    public string[] Names => ["list"];

    public void Execute(Command.Command.CommandSource c)
    {
        c.Output.SendMessage("Connected players: " + c.Server.playerManager.getPlayerList());
    }
}
