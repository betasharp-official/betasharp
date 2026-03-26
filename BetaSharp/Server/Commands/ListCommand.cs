using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class ListCommand : ICommand
{
    public string Usage => "list";
    public string Description => "Lists connected players";
    public string[] Names => ["list"];

    public void Execute(ICommand.CommandContext c)
    {
        c.Output.SendMessage("Connected players: " + c.Server.playerManager.getPlayerList());
    }
}
