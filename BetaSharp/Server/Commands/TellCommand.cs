using BetaSharp.Network.Packets.Play;
using BetaSharp.Server.Command;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Commands;

public class TellCommand : ICommand
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(TellCommand));

    public string Usage => "tell <player> <message>";
    public string Description => "Whispers to a player";
    public string[] Names => ["tell"];

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 2)
        {
            c.Output.SendMessage("Usage: tell <player> <message>");
            return;
        }

        string targetName = c.Args[0];
        string message = string.Join(" ", c.Args[1..]);
        s_logger.LogInformation("[" + c.SenderName + "->" + targetName + "] " + message);

        string whisper = "§7" + c.SenderName + " whispers " + message;
        s_logger.LogInformation(whisper);

        if (!c.Server.playerManager.sendPacket(targetName, ChatMessagePacket.Get(whisper)))
        {
            c.Output.SendMessage("There's no player by that name online.");
        }
    }
}
