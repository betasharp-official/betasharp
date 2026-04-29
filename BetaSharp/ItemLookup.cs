using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BetaSharp.Blocks;
using BetaSharp.Items;

namespace BetaSharp;

internal static class ItemLookup
{
    private static readonly Dictionary<string, int> s_itemNameToId = [];
    private static bool s_lookupTablesBuilt;

    public static void Initialize() => BuildItemLookupTables();

    internal static bool TryGetItemId(string input, out int itemId)
    {
        int colon = input.IndexOf(':');
        if (colon < 0) return TryGetItemId(input, out itemId, false);

        string a = input[..colon];
        string b = input[(colon + 1)..];
        if (!int.TryParse(b, out _))
        {
            // Ignore item meta/damage
            TryGetItemId(a, out itemId, false);
        }

        // found namespace
        return TryGetItemId(Namespace.Get(a), b, out itemId);
    }

    // TODO: respect namespace in item ID lookup.
    internal static bool TryGetItemId(Namespace @namespace, string input, out int itemId) =>
        TryGetItemId(input, out itemId, true);

    private static bool TryGetItemId(string input, out int itemId, bool haveNamespace)
    {
        if (int.TryParse(input, out itemId))
        {
            return itemId >= 0 && itemId < Item.ITEMS.Length && Item.ITEMS[itemId] != null;
        }

        return s_itemNameToId.TryGetValue(input.ToLower(), out itemId);
    }

    /// <summary>
    /// Parses "id", "name", "id:damage", or "name:damage" into an ItemStack.
    /// </summary>
    internal static bool TryGetItem(string input, [NotNullWhen(true)] out ItemStack? itemId, int itemCount = 1, int meta = 0) =>
        TryGetItem(input, out itemId, itemCount, meta, false);

    private static bool TryGetItem(string input, [NotNullWhen(true)] out ItemStack? itemId, int itemCount, int meta, bool haveNamespace)
    {
        int colon = input.IndexOf(':');

        string name;
        int damage;

        if (colon < 0)
        {
            name = input;
            damage = meta;
        }
        else
        {
            name = input[..colon];
            string damageStr = input[(colon + 1)..];
            if (!int.TryParse(damageStr, out damage))
            {
                if (haveNamespace)
                {
                    throw new Exception("Inavlid item meta. Expected number found \"" + damageStr + "\"");
                }

                // damageStr is the item name.
                // name is the namespace.
                return TryGetItem(Namespace.Get(name), damageStr, out itemId, itemCount, meta);
            }
        }

        if (!TryGetItemId(name, out int id))
        {
            itemId = null;
            return false;
        }

        itemId = new ItemStack(id, itemCount, damage);
        return true;
    }

    // TODO: respect namespace in item lookup.
    internal static bool TryGetItem(Namespace @namespace, string input, [NotNullWhen(true)] out ItemStack? itemId, int itemCount = 1, int meta = 0) =>
        TryGetItem(input, out itemId, itemCount, meta, true);

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

        IEnumerable<FieldInfo> itemFields = typeof(Item).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType.IsAssignableTo(typeof(Item)));
        foreach (FieldInfo field in itemFields)
        {
            if (field.GetValue(null) is Item item)
            {
                s_itemNameToId.TryAdd(field.Name.ToLower(), item.id);
            }
        }

        IEnumerable<FieldInfo> blockFields = typeof(Block).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType.IsAssignableTo(typeof(Block)));
        foreach (FieldInfo field in blockFields)
        {
            if (field.GetValue(null) is Block block)
            {
                s_itemNameToId.TryAdd(field.Name.ToLower(), block.id);
            }
        }
    }
}
