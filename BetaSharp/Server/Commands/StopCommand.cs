using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class StopCommand : ICommand
{
    public string Usage => "stop";
    public string Description => "Stops the server";
    public string[] Names => ["stop"];
    public byte PermissionLevel => 4;
    public bool DisallowInternalServer => true;

    public void Execute(ICommand.CommandContext c)
    {
        c.LogOp("Stopping the server..");
        c.Server.Stop();
    }
}
