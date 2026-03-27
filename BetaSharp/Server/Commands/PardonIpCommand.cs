using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class PardonIpCommand : ICommand
{
    public string Usage => "pardon-ip <ip>";
    public string Description => "Pardons an IP address";
    public string[] Names => ["pardon-ip"];
    public bool DisallowInternalServer => true;

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: pardon-ip <ip>");
            return;
        }

        string ip = c.Args[0];
        c.Server.playerManager.unbanIp(ip);
        c.LogOp("Pardoning ip " + ip);
    }
}
