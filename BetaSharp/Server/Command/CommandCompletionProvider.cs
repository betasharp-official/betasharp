using BetaSharp.Server.Commands;

namespace BetaSharp.Server.Command;

internal static class CommandCompletionProvider
{
    /// <summary>
    /// Get tab completions for command arguments
    /// </summary>
    public static List<string> GetCompletions(string commandName, int argIndex, string currentArgPrefix, BetaSharpServer server)
    {
        // Normalize command name
        commandName = commandName.ToLower().Substring(1); // Remove leading "/"

        var completions = commandName switch
        {
            "give" => GetGiveCompletions(argIndex, currentArgPrefix, server),
            "tp" or "teleport" => GetTeleportCompletions(argIndex, currentArgPrefix, server),
            "say" => GetSayCompletions(argIndex, currentArgPrefix, server),
            "tell" => GetTellCompletions(argIndex, currentArgPrefix, server),
            "kick" or "ban" or "op" or "deop" or "pardon" => GetPlayerCompletions(currentArgPrefix, server),
            _ => []
        };

        return completions
            .Where(c => string.IsNullOrEmpty(currentArgPrefix) || c.StartsWith(currentArgPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c)
            .ToList();
    }

    private static List<string> GetGiveCompletions(int argIndex, string currentArgPrefix, BetaSharpServer server)
    {
        return argIndex switch
        {
            0 => GetPlayerCompletions(currentArgPrefix, server), // First arg: player name
            1 => GetItemNameCompletions(currentArgPrefix),      // Second arg: item name
            2 => [],                                             // Third arg: count (number)
            _ => []
        };
    }

    private static List<string> GetTeleportCompletions(int argIndex, string currentArgPrefix, BetaSharpServer server)
    {
        return argIndex switch
        {
            0 => GetPlayerCompletions(currentArgPrefix, server), // First arg: player name
            _ => [] // x, y, z are numbers
        };
    }

    private static List<string> GetSayCompletions(int argIndex, string currentArgPrefix, BetaSharpServer server)
    {
        // /say doesn't really have meaningful completions
        return [];
    }

    private static List<string> GetTellCompletions(int argIndex, string currentArgPrefix, BetaSharpServer server)
    {
        return argIndex switch
        {
            0 => GetPlayerCompletions(currentArgPrefix, server), // First arg: player name
            _ => []
        };
    }

    private static List<string> GetPlayerCompletions(string prefix, BetaSharpServer server)
    {
        if (server?.playerManager?.players == null)
        {
            return [];
        }

        return server.playerManager.players
            .Select(p => p?.name)
            .Where(name => !string.IsNullOrEmpty(name) &&
                          (string.IsNullOrEmpty(prefix) || name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(name => name)
            .ToList();
    }

    private static List<string> GetItemNameCompletions(string prefix)
    {
        return ItemLookup.GetAvailableItemNames(prefix);
    }
}
