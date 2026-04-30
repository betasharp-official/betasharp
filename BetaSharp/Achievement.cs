using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.Stats;

namespace BetaSharp;

public class Achievement : StatBase
{
    public readonly int column;
    public readonly int row;
    public readonly Achievement parent;
    public string TranslationKey { get; }
    public readonly ItemStack icon;
    private bool _isChallenge;
    public Func<string>? GetTranslatedDescription { get; set; }
  
    // TODO: Remove this null-safe fallback once static bootstrap order is refactored
    // so Achievement icon items are always fully initialized before construction.
    public Achievement(int id, string key, int column, int row, Item? displayItem, Achievement parent) : this(id, key, column, row, new ItemStack(displayItem?.id ?? 280, 1, 0), parent)
    {
    }

    public Achievement(int id, string key, int column, int row, Block displayBlock, Achievement parent) : this(id, key, column, row, new ItemStack(displayBlock), parent)
    {
    }

    public Achievement(int id, string key, int column, int row, ItemStack icon, Achievement parent) : base(5242880 + id, StatCollector.TranslateToLocal("achievement." + key))
    {
        this.icon = icon;
        TranslationKey = StatCollector.TranslateToLocal("achievement." + key + ".desc");
        this.column = column;
        this.row = row;
        if (column < Achievements.minColumn)
        {
            Achievements.minColumn = column;
        }

        if (row < Achievements.minRow)
        {
            Achievements.minRow = row;
        }

        if (column > Achievements.maxColumn)
        {
            Achievements.maxColumn = column;
        }

        if (row > Achievements.maxRow)
        {
            Achievements.maxRow = row;
        }

        this.parent = parent;
    }

    public Achievement m_66876377()
    {
        LocalOnly = true;
        return this;
    }

    public Achievement challenge()
    {
        _isChallenge = true;
        return this;
    }

    public Achievement registerAchievement()
    {
        base.RegisterStat();
        Achievements.AllAchievements.Add(this);
        return this;
    }

    public override bool IsAchievement()
    {
        return true;
    }

    public string? getTranslatedDescription()
    {
        if (GetTranslatedDescription == null)
        {
            return TranslationKey;
        }

        return GetTranslatedDescription();
    }

    public bool isChallenge()
    {
        return _isChallenge;
    }

    public override StatBase RegisterStat()
    {
        return registerAchievement();
    }

    public override StatBase SetLocalOnly()
    {
        return m_66876377();
    }
}
