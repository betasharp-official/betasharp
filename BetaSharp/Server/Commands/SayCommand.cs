using BetaSharp.Network.Packets.Play;
using BetaSharp.Server.Command;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Commands;

public class SayCommand : Command.Command
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(SayCommand));

    public string Usage => "say <message>";
    public string Description => "Broadcasts a message";
    public string[] Names => ["say"];

    public void Execute(Command.Command.CommandSource c)
    {
        if (c.Args.Length == 0) return;

        string message = string.Join(" ", c.Args);
        s_logger.LogInformation("[" + c.SenderName + "] " + message);
        c.Server.playerManager.sendToAll(ChatMessagePacket.Get("§d[Server] " + message));
    }
}
