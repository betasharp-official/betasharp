using BetaSharp.Entities;
using BetaSharp.Server.Internal;
using java.util.logging;

namespace BetaSharp.Server.Commands;

internal class AdminCommands
{
    private static readonly Logger logger = Logger.getLogger("BetaSharp");

    public static void List(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        output.SendMessage("Connected players: " + server.playerManager.getPlayerList());
    }

    public static void Stop(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;

        LogCommand(server, senderName, "Stopping the server..");
        server.stop();
    }

    public static void SaveAll(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        LogCommand(server, senderName, "Forcing save..");
        server.playerManager?.savePlayers();

        for (int i = 0; i < server.worlds.Length; i++)
        {
            server.worlds[i].saveWithLoadingDisplay(true, null);
        }

        LogCommand(server, senderName, "Save complete.");
    }

    public static void SaveToggle(BetaSharpServer server, string senderName, bool disable)
    {
        string action = disable ? "Disabling" : "Enabling";
        LogCommand(server, senderName, $"{action} level saving..");

        for (int i = 0; i < server.worlds.Length; i++)
        {
            server.worlds[i].savingDisabled = disable;
        }
    }

    public static void Op(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: op <player>"); return; }

        string target = args[0];
        server.playerManager.addToOperators(target);
        LogCommand(server, senderName, "Opping " + target);
        server.playerManager.messagePlayer(target, "§eYou are now op!");
    }

    public static void Deop(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: deop <player>"); return; }

        string target = args[0];
        server.playerManager.removeFromOperators(target);
        server.playerManager.messagePlayer(target, "§eYou are no longer op!");
        LogCommand(server, senderName, "De-opping " + target);
    }

    public static void Ban(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: ban <player>"); return; }

        string target = args[0];
        server.playerManager.banPlayer(target);
        LogCommand(server, senderName, "Banning " + target);
        server.playerManager.getPlayer(target)?.networkHandler.disconnect("Banned by admin");
    }

    public static void Pardon(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: pardon <player>"); return; }

        string target = args[0];
        server.playerManager.unbanPlayer(target);
        LogCommand(server, senderName, "Pardoning " + target);
    }

    public static void BanIp(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: ban-ip <ip>"); return; }

        string ip = args[0];
        server.playerManager.banIp(ip);
        LogCommand(server, senderName, "Banning ip " + ip);
    }

    public static void PardonIp(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: pardon-ip <ip>"); return; }

        string ip = args[0];
        server.playerManager.unbanIp(ip);
        LogCommand(server, senderName, "Pardoning ip " + ip);
    }

    public static void Kick(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: kick <player>"); return; }

        string target = args[0];
        ServerPlayerEntity targetPlayer = server.playerManager.getPlayer(target);

        if (targetPlayer != null)
        {
            targetPlayer.networkHandler.disconnect("Kicked by admin");
            LogCommand(server, senderName, "Kicking " + targetPlayer.name);
        }
        else
        {
            output.SendMessage("Can't find user " + target + ". No kick.");
        }
    }

    public static void Whitelist(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1)
        {
            output.SendMessage("Usage: whitelist <on|off|list|add|remove|reload> [player]");
            return;
        }

        string action = args[0].ToLower();
        switch (action)
        {
            case "on":
                LogCommand(server, senderName, "Turned on white-listing");
                server.config.SetProperty("white-list", true);
                break;
            case "off":
                LogCommand(server, senderName, "Turned off white-listing");
                server.config.SetProperty("white-list", false);
                break;
            case "list":
                var whitelist = server.playerManager.getWhitelist();
                string names = "";
                foreach (string name in whitelist)
                {
                    names += name + " ";
                }
                output.SendMessage("White-listed players: " + names);
                break;
            case "add" when args.Length >= 2:
                string addTarget = args[1].ToLower();
                server.playerManager.addToWhitelist(addTarget);
                LogCommand(server, senderName, "Added " + addTarget + " to white-list");
                break;
            case "remove" when args.Length >= 2:
                string removeTarget = args[1].ToLower();
                server.playerManager.removeFromWhitelist(removeTarget);
                LogCommand(server, senderName, "Removed " + removeTarget + " from white-list");
                break;
            case "reload":
                server.playerManager.reloadWhitelist();
                LogCommand(server, senderName, "Reloaded white-list from file");
                break;
        }
    }

    private static bool IsInternalServer(BetaSharpServer server, CommandOutput output)
    {
        if (server is InternalServer)
        {
            output.SendMessage("This command is not available in singleplayer.");
            return true;
        }
        return false;
    }

    internal static void LogCommand(BetaSharpServer server, string senderName, string message)
    {
        string logMessage = senderName + ": " + message;
        server.playerManager.broadcast("§7(" + logMessage + ")");
        logger.info(logMessage);
    }
}
