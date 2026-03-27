using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class BanIpCommand : ICommand
{
    public string Usage => "ban-ip <ip>";
    public string Description => "Bans an IP address";
    public string[] Names => ["ban-ip"];
    public bool DisallowInternalServer => true;

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: ban-ip <ip>");
            return;
        }

        string ip = c.Args[0];
        c.Server.playerManager.banIp(ip);
        c.LogOp("Banning ip " + ip);
    }
}
