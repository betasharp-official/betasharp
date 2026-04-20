using System.Reflection;
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
        if (int.TryParse(input, out itemId))
        {
            return itemId >= 0 && itemId < Item.ITEMS.Length && Item.ITEMS[itemId] != null;
        }

        return s_itemNameToId.TryGetValue(input.ToLower(), out itemId);
    }

    internal static string ResolveItemName(ItemStack item) =>
        s_itemNameToId.FirstOrDefault(kvp => kvp.Value == item.ItemId).Key ?? item.getItemName();

    /// <summary>
    /// Gets all available item names that start with the given prefix (with underscores)
    /// </summary>
    public static List<string> GetAvailableItemNames(string prefix = "")
    {
        if (!s_lookupTablesBuilt)
        {
            BuildItemLookupTables();
        }

        return s_itemNameToId.Keys
            .Where(name => string.IsNullOrEmpty(prefix) || name.StartsWith(prefix.ToLower()))
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
                s_itemNameToId.TryAdd(field.Name.ToLower(), block.ID);
            }
        }

        IEnumerable<FieldInfo> itemFields = typeof(Item).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType.IsAssignableTo(typeof(Item)));
        foreach (FieldInfo field in itemFields)
        {
            if (field.GetValue(null) is Item item)
            {
                s_itemNameToId.TryAdd(field.Name.ToLower(), item.id);
            }
        }
    }
}
