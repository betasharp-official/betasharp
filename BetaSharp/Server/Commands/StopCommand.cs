using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class StopCommand : Command.Command
{
    public string Usage => "stop";
    public string Description => "Stops the server";
    public string[] Names => ["stop"];
    public byte PermissionLevel => 4;
    public bool DisallowInternalServer => true;

    public void Execute(Command.Command.CommandSource c)
    {
        c.LogOp("Stopping the server..");
        c.Server.Stop();
    }
}
