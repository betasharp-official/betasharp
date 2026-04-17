using System.Reflection;
using System.Runtime.CompilerServices;
using BetaSharp.Blocks;
using BetaSharp.Items;

namespace BetaSharp.Server.Command;

internal static class ItemLookup
{
    private static readonly Dictionary<string, int> s_itemNameToId = [];
    private static bool s_lookupTablesBuilt;

    public static void Initialize() => BuildItemLookupTables();

    internal static bool TryResolveItemId(string input, out int itemId)
    {
        if (!s_lookupTablesBuilt)
        {
            BuildItemLookupTables();
        }

        if (int.TryParse(input, out itemId))
        {
            return itemId >= 0 && itemId < Item.ITEMS.Length && Item.ITEMS[itemId] != null;
        }

        return s_itemNameToId.TryGetValue(NormalizeName(input), out itemId);
    }

    internal static string ResolveItemName(ItemStack item)
    {
        if (!s_lookupTablesBuilt)
        {
            BuildItemLookupTables();
        }

        return s_itemNameToId.FirstOrDefault(kvp => kvp.Value == item.ItemId).Key ?? item.getItemName();
    }

    /// <summary>
    /// Gets all available item names that start with the given prefix (with underscores)
    /// </summary>
    public static List<string> GetAvailableItemNames(string prefix = "")
    {
        if (!s_lookupTablesBuilt)
        {
            BuildItemLookupTables();
        }

        string normalizedPrefix = NormalizeName(prefix);

        return s_itemNameToId.Keys
            .Where(name => string.IsNullOrEmpty(normalizedPrefix) || name.StartsWith(normalizedPrefix))
            .OrderBy(name => name)
            .ToList();
    }

    private static void BuildItemLookupTables()
    {
        if (s_lookupTablesBuilt)
        {
            return;
        }

        s_lookupTablesBuilt = true;

        IEnumerable<FieldInfo> blockFields = typeof(Block).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType.IsAssignableTo(typeof(Block)));
        foreach (FieldInfo field in blockFields)
        {
            if (field.GetValue(null) is Block block)
            {
                AddName(field.Name, block.id, overwrite: false);
                AddName(block.getBlockName(), block.id, overwrite: false);
            }
        }

        IEnumerable<FieldInfo> itemFields = typeof(Item).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType.IsAssignableTo(typeof(Item)));
        foreach (FieldInfo field in itemFields)
        {
            if (field.GetValue(null) is Item item)
            {
                AddName(field.Name, item.id, overwrite: true);
                AddName(item.getItemName(), item.id, overwrite: true);
            }
        }

        AddAlias("door", Item.WoodenDoor.id);
        AddAlias("wooddoor", Item.WoodenDoor.id);
        AddAlias("woodendoor", Item.WoodenDoor.id);
        AddAlias("irondoor", Item.IronDoor.id);
        AddAlias("sugarcane", Item.SugarCane.id);
    }

    private static void AddAlias(string name, int itemId)
    {
        AddName(name, itemId, overwrite: true);
    }

    private static void AddName(string name, int itemId, bool overwrite)
    {
        string normalized = NormalizeName(name);
        if (string.IsNullOrEmpty(normalized))
        {
            return;
        }

        if (overwrite)
        {
            s_itemNameToId[normalized] = itemId;
        }
        else
        {
            s_itemNameToId.TryAdd(normalized, itemId);
        }
    }

    private static string NormalizeName(string name)
    {
        string value = name.Trim().ToLowerInvariant();

        if (value.StartsWith("item.", StringComparison.Ordinal))
        {
            value = value[5..];
        }
        else if (value.StartsWith("tile.", StringComparison.Ordinal))
        {
            value = value[5..];
        }

        return new string(value.Where(char.IsLetterOrDigit).ToArray());
    }
}
