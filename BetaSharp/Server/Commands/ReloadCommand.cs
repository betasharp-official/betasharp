using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class ReloadCommand : Command.Command
{
    public string Usage => "reload";
    public string Description => "Reloads all datapacks";
    public string[] Names => ["reload"];
    public byte PermissionLevel => 4;

    public void Execute(Command.Command.CommandSource c)
    {
        c.Server.ReloadDatapacks();
    }
}
