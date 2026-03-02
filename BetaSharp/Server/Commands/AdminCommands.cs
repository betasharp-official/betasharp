using BetaSharp.Entities;
using BetaSharp.Server.Internal;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Commands;

internal class AdminCommands
{
    private static readonly ILogger s_logger = Log.Instance.For<AdminCommands>();

    public static void List(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        output.SendMessage("Connected players: " + server.PlayerManager.GetPlayerList());
    }

    public static void Stop(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;

        LogCommand(server, senderName, "Stopping the server..");
        server.Stop();
    }

    public static void SaveAll(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        LogCommand(server, senderName, "Forcing save..");
        server.PlayerManager?.SavePlayers();

        for (int i = 0; i < server.Worlds.Length; i++)
        {
            server.Worlds[i].saveWithLoadingDisplay(true, null);
        }

        LogCommand(server, senderName, "Save complete.");
    }

    public static void SaveToggle(MinecraftServer server, string senderName, bool disable)
    {
        string action = disable ? "Disabling" : "Enabling";
        LogCommand(server, senderName, $"{action} level saving..");

        for (int i = 0; i < server.Worlds.Length; i++)
        {
            server.Worlds[i].savingDisabled = disable;
        }
    }

    public static void Op(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: op <player>"); return; }

        string target = args[0];
        server.PlayerManager.AddToOperators(target);
        LogCommand(server, senderName, "Opping " + target);
        server.PlayerManager.MessagePlayer(target, "Â§eYou are now op!");
    }

    public static void Deop(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: deop <player>"); return; }

        string target = args[0];
        server.PlayerManager.RemoveFromOperators(target);
        server.PlayerManager.MessagePlayer(target, "Â§eYou are no longer op!");
        LogCommand(server, senderName, "De-opping " + target);
    }

    public static void Ban(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: ban <player>"); return; }

        string target = args[0];
        server.PlayerManager.BanPlayer(target);
        LogCommand(server, senderName, "Banning " + target);
        server.PlayerManager.GetPlayer(target)?.networkHandler.Disconnect("Banned by admin");
    }

    public static void Pardon(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: pardon <player>"); return; }

        string target = args[0];
        server.PlayerManager.UnbanPlayer(target);
        LogCommand(server, senderName, "Pardoning " + target);
    }

    public static void BanIp(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: ban-ip <ip>"); return; }

        string ip = args[0];
        server.PlayerManager.BanIp(ip);
        LogCommand(server, senderName, "Banning ip " + ip);
    }

    public static void PardonIp(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: pardon-ip <ip>"); return; }

        string ip = args[0];
        server.PlayerManager.UnbanIp(ip);
        LogCommand(server, senderName, "Pardoning ip " + ip);
    }

    public static void Kick(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (IsInternalServer(server, output)) return;
        if (args.Length < 1) { output.SendMessage("Usage: kick <player>"); return; }

        string target = args[0];
        ServerPlayerEntity targetPlayer = server.PlayerManager.GetPlayer(target);

        if (targetPlayer != null)
        {
            targetPlayer.networkHandler.Disconnect("Kicked by admin");
            LogCommand(server, senderName, "Kicking " + targetPlayer.name);
        }
        else
        {
            output.SendMessage("Can't find user " + target + ". No kick.");
        }
    }

    public static void Whitelist(MinecraftServer server, string senderName, string[] args, CommandOutput output)
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
                server.Config.SetProperty("white-list", true);
                break;
            case "off":
                LogCommand(server, senderName, "Turned off white-listing");
                server.Config.SetProperty("white-list", false);
                break;
            case "list":
                var whitelist = server.PlayerManager.GetWhitelist();
                string names = "";
                foreach (string name in whitelist)
                {
                    names += name + " ";
                }
                output.SendMessage("White-listed players: " + names);
                break;
            case "add" when args.Length >= 2:
                string addTarget = args[1].ToLower();
                server.PlayerManager.AddToWhitelist(addTarget);
                LogCommand(server, senderName, "Added " + addTarget + " to white-list");
                break;
            case "remove" when args.Length >= 2:
                string removeTarget = args[1].ToLower();
                server.PlayerManager.RemoveFromWhitelist(removeTarget);
                LogCommand(server, senderName, "Removed " + removeTarget + " from white-list");
                break;
            case "reload":
                server.PlayerManager.ReloadWhitelist();
                LogCommand(server, senderName, "Reloaded white-list from file");
                break;
        }
    }

    private static bool IsInternalServer(MinecraftServer server, CommandOutput output)
    {
        if (server is InternalServer)
        {
            output.SendMessage("This command is not available in singleplayer.");
            return true;
        }
        return false;
    }

    internal static void LogCommand(MinecraftServer server, string senderName, string message)
    {
        string logMessage = senderName + ": " + message;
        server.PlayerManager.Broadcast("Â§7(" + logMessage + ")");
        s_logger.LogInformation(logMessage);
    }
}
