using BetaSharp.Network.Packets.Play;
using java.util.logging;

namespace BetaSharp.Server.Commands;

internal static class ChatCommands
{
    private static readonly Logger logger = Logger.getLogger("Minecraft");

    public static void Say(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length == 0) return;

        string message = string.Join(" ", args);
        logger.info("[" + senderName + "] " + message);
        server.playerManager.sendToAll(new ChatMessagePacket("§d[Server] " + message));
    }

    public static void Tell(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length < 2)
        {
            output.SendMessage("Usage: tell <player> <message>");
            return;
        }

        string targetName = args[0];
        string message = string.Join(" ", args[1..]);
        logger.info("[" + senderName + "->" + targetName + "] " + message);

        string whisper = "§7" + senderName + " whispers " + message;
        logger.info(whisper);

        if (!server.playerManager.sendPacket(targetName, new ChatMessagePacket(whisper)))
        {
            output.SendMessage("There's no player by that name online.");
        }
    }
}
