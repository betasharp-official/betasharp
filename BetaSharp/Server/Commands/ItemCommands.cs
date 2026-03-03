using System.Reflection;
using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Server.Commands;

internal static class ItemCommands
{
    private static readonly Dictionary<string, int> itemNameToId = [];
    private static bool lookupTablesBuilt;

    public static void Initialize()
    {
        BuildItemLookupTables();
    }

    public static void Give(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length < 1) { output.SendMessage("Usage: give <item> [count]"); return; }

        if (TryResolveItemId(args[0], out int selfItemId))
        {
            ServerPlayerEntity sender = server.playerManager.getPlayer(senderName);
            if (sender == null) { output.SendMessage("Could not find your player."); return; }

            int count = 1;
            if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
            {
                count = Math.Clamp(parsedCount, 1, 64);
            }

            sender.dropItem(new ItemStack(selfItemId, count, 0));
            output.SendMessage($"Gave {count} of {args[0]} (id: {selfItemId})");
            return;
        }

        if (args.Length >= 2)
        {
            string targetName = args[0];
            ServerPlayerEntity targetPlayer = server.playerManager.getPlayer(targetName);

            if (targetPlayer == null)
            {
                output.SendMessage("Can't find user " + targetName);
                return;
            }

            if (!TryResolveItemId(args[1], out int itemId))
            {
                output.SendMessage("Unknown item: " + args[1]);
                return;
            }

            if (Item.ITEMS[itemId] == null)
            {
                output.SendMessage("There's no item with id " + itemId);
                return;
            }

            int count = 1;
            if (args.Length > 2 && int.TryParse(args[2], out int parsedCount))
            {
                count = Math.Clamp(parsedCount, 1, 64);
            }

            AdminCommands.LogCommand(server, senderName, $"Giving {targetPlayer.name} some {itemId}");
            targetPlayer.dropItem(new ItemStack(itemId, count, 0));
            return;
        }

        output.SendMessage("Usage: give <item> [count]  or  give <player> <id> [count]");
    }

    internal static bool TryResolveItemId(string input, out int itemId)
    {
        if (int.TryParse(input, out itemId))
        {
            return itemId >= 0 && itemId < Item.ITEMS.Length && Item.ITEMS[itemId] != null;
        }

        return itemNameToId.TryGetValue(input.ToLower(), out itemId);
    }

    /// <summary>
    /// Gets all available item names that start with the given prefix (with underscores)
    /// </summary>
    public static List<string> GetAvailableItemNames(string prefix = "")
    {
        if (!lookupTablesBuilt)
        {
            BuildItemLookupTables();
        }

        return itemNameToId.Keys
            .Where(name => string.IsNullOrEmpty(prefix) || name.StartsWith(prefix.ToLower()))
            .OrderBy(name => name)
            .ToList();
    }

    private static void BuildItemLookupTables()
    {
        if (lookupTablesBuilt) return;
        lookupTablesBuilt = true;

        var blockFields = typeof(Block).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(Block));
        foreach (var field in blockFields)
        {
            if (field.GetValue(null) is Block block)
            {
                itemNameToId.TryAdd(field.Name.ToLower(), block.id);
            }
        }

        var itemFields = typeof(Item).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(Item));
        foreach (var field in itemFields)
        {
            if (field.GetValue(null) is Item item)
            {
                itemNameToId.TryAdd(field.Name.ToLower(), item.id);
            }
        }
    }
}
