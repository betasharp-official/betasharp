using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.Recipes;

namespace BetaSharp.Stats;

public static class Stats
{
    public static Dictionary<int, StatBase> IdToStat = [];
    public static List<StatBase> AllStats = [];
    public static List<StatBase> GeneralStats = [];
    public static List<StatBase> ItemStats = [];
    public static List<StatBase> BlocksMinedStats = [];

    public static StatBase StartGameStat = new StatBasic(1000, StatCollector.TranslateToLocal("stat.startGame")).SetLocalOnly().RegisterStat();
    public static StatBase CreateWorldStat = new StatBasic(1001, StatCollector.TranslateToLocal("stat.createWorld")).SetLocalOnly().RegisterStat();
    public static StatBase LoadWorldStat = new StatBasic(1002, StatCollector.TranslateToLocal("stat.loadWorld")).SetLocalOnly().RegisterStat();
    public static StatBase JoinMultiplayerStat = new StatBasic(1003, StatCollector.TranslateToLocal("stat.joinMultiplayer")).SetLocalOnly().RegisterStat();
    public static StatBase LeaveGameStat = new StatBasic(1004, StatCollector.TranslateToLocal("stat.leaveGame")).SetLocalOnly().RegisterStat();
    public static StatBase MinutesPlayedStat = new StatBasic(1100, StatCollector.TranslateToLocal("stat.playOneMinute"), StatFormatters.FormatTime).SetLocalOnly().RegisterStat();
    public static StatBase DistanceWalkedStat = new StatBasic(2000, StatCollector.TranslateToLocal("stat.walkOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase DistanceSwumStat = new StatBasic(2001, StatCollector.TranslateToLocal("stat.swimOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase DistanceFallenStat = new StatBasic(2002, StatCollector.TranslateToLocal("stat.fallOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase DistanceClimbedStat = new StatBasic(2003, StatCollector.TranslateToLocal("stat.climbOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase DistanceFlownStat = new StatBasic(2004, StatCollector.TranslateToLocal("stat.flyOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase DistanceDoveStat = new StatBasic(2005, StatCollector.TranslateToLocal("stat.diveOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase DistanceByMinecartStat = new StatBasic(2006, StatCollector.TranslateToLocal("stat.minecartOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase DistanceByBoatStat = new StatBasic(2007, StatCollector.TranslateToLocal("stat.boatOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase DistanceByPigStat = new StatBasic(2008, StatCollector.TranslateToLocal("stat.pigOneCm"), StatFormatters.FormatDistance).SetLocalOnly().RegisterStat();
    public static StatBase JumpStat = new StatBasic(2010, StatCollector.TranslateToLocal("stat.jump")).SetLocalOnly().RegisterStat();
    public static StatBase DropStat = new StatBasic(2011, StatCollector.TranslateToLocal("stat.drop")).SetLocalOnly().RegisterStat();
    public static StatBase DamageDealtStat = new StatBasic(2020, StatCollector.TranslateToLocal("stat.damageDealt")).RegisterStat();
    public static StatBase DamageTakenStat = new StatBasic(2021, StatCollector.TranslateToLocal("stat.damageTaken")).RegisterStat();
    public static StatBase DeathsStat = new StatBasic(2022, StatCollector.TranslateToLocal("stat.deaths")).RegisterStat();
    public static StatBase MobKillsStat = new StatBasic(2023, StatCollector.TranslateToLocal("stat.mobKills")).RegisterStat();
    public static StatBase PlayerKillsStat = new StatBasic(2024, StatCollector.TranslateToLocal("stat.playerKills")).RegisterStat();
    public static StatBase FishCaughtStat = new StatBasic(2025, StatCollector.TranslateToLocal("stat.fishCaught")).RegisterStat();

    public static StatBase[] MineBlockStatArray = InitBlocksMined("stat.mineBlock", 16777216);
    public static StatBase[] Crafted;
    public static StatBase[] Used;
    public static StatBase[] Broken;

    private static bool _hasBasicItemStatsInitialized;
    private static bool _hasExtendedItemStatsInitialized;

    public static void InitializeItemStats()
    {
        Used = InitItemUsedStats(Used, "stat.useItem", 16908288, 0, Block.Blocks.Length);
        Broken = InitializeBrokenItemStats(Broken, "stat.breakItem", 16973824, 0, Block.Blocks.Length);
        _hasBasicItemStatsInitialized = true;
        InitializeCraftedItemStats();
    }

    public static void InitializeExtendedItemStats()
    {
        Used = InitItemUsedStats(Used, "stat.useItem", 16908288, Block.Blocks.Length, 32000);
        Broken = InitializeBrokenItemStats(Broken, "stat.breakItem", 16973824, Block.Blocks.Length, 32000);
        _hasExtendedItemStatsInitialized = true;
        InitializeCraftedItemStats();
    }

    public static void InitializeCraftedItemStats()
    {
        if (_hasBasicItemStatsInitialized && _hasExtendedItemStatsInitialized)
        {
            HashSet<int> craftedIds = new HashSet<int>();

            foreach (IRecipe recipe in CraftingManager.getInstance().Recipes)
            {
                craftedIds.Add(recipe.GetRecipeOutput().ItemId);
            }

            foreach (ItemStack itemStack in SmeltingRecipeManager.getInstance().GetSmeltingList().Values)
            {
                craftedIds.Add(itemStack.ItemId);
            }

            Crafted = new StatBase[32000];

            foreach (int itemId in craftedIds)
            {
                if (Item.ITEMS[itemId] != null)
                {
                    string translatedName = StatCollector.TranslateToLocalFormatted("stat.craftItem", Item.ITEMS[itemId].getStatName());
                    Crafted[itemId] = new StatCrafting(16842752 + itemId, translatedName, itemId).RegisterStat();
                }
            }

            ReplaceAllSimilarBlocks(Crafted);
        }
    }

    private static StatBase[] InitBlocksMined(string baseName, int baseId)
    {
        StatBase[] statsArray = new StatBase[256];

        for (int i = 0; i < 256; ++i)
        {
            if (Block.Blocks[i] != null && Block.Blocks[i].GetEnableStats())
            {
                string translatedName = StatCollector.TranslateToLocalFormatted(baseName, Block.Blocks[i].TranslateBlockName());
                statsArray[i] = new StatCrafting(baseId + i, translatedName, i).RegisterStat();
                BlocksMinedStats.Add(statsArray[i]);
            }
        }

        ReplaceAllSimilarBlocks(statsArray);
        return statsArray;
    }

    private static StatBase[] InitItemUsedStats(StatBase[] statsArray, string baseName, int baseId, int startIdx, int endIdx)
    {
        statsArray ??= new StatBase[32000];

        for (int i = startIdx; i < endIdx; ++i)
        {
            if (Item.ITEMS[i] != null)
            {
                string translatedName = StatCollector.TranslateToLocalFormatted(baseName, Item.ITEMS[i].getStatName());
                statsArray[i] = new StatCrafting(baseId + i, translatedName, i).RegisterStat();

                if (i >= Block.Blocks.Length)
                {
                    ItemStats.Add(statsArray[i]);
                }
            }
        }

        ReplaceAllSimilarBlocks(statsArray);
        return statsArray;
    }

    private static StatBase[] InitializeBrokenItemStats(StatBase[] statsArray, string baseName, int baseId, int startIdx, int endIdx)
    {
        statsArray ??= new StatBase[32000];

        for (int i = startIdx; i < endIdx; ++i)
        {
            if (Item.ITEMS[i] != null && Item.ITEMS[i].isDamagable())
            {
                string translatedName = StatCollector.TranslateToLocalFormatted(baseName, Item.ITEMS[i].getStatName());
                statsArray[i] = new StatCrafting(baseId + i, translatedName, i).RegisterStat();
            }
        }

        ReplaceAllSimilarBlocks(statsArray);
        return statsArray;
    }

    private static void ReplaceAllSimilarBlocks(StatBase[] statsArray)
    {
        ReplaceSimilarBlocks(statsArray, Block.Water.ID, Block.FlowingWater.ID);
        ReplaceSimilarBlocks(statsArray, Block.Lava.ID, Block.Lava.ID);
        ReplaceSimilarBlocks(statsArray, Block.JackLantern.ID, Block.Pumpkin.ID);
        ReplaceSimilarBlocks(statsArray, Block.LitFurnace.ID, Block.Furnace.ID);
        ReplaceSimilarBlocks(statsArray, Block.LitRedstoneOre.ID, Block.RedstoneOre.ID);
        ReplaceSimilarBlocks(statsArray, Block.PoweredRepeater.ID, Block.Repeater.ID);
        ReplaceSimilarBlocks(statsArray, Block.LitRedstoneTorch.ID, Block.RedstoneTorch.ID);
        ReplaceSimilarBlocks(statsArray, Block.RedMushroom.ID, Block.BrownMushroom.ID);
        ReplaceSimilarBlocks(statsArray, Block.DoubleSlab.ID, Block.Slab.ID);
        ReplaceSimilarBlocks(statsArray, Block.GrassBlock.ID, Block.Dirt.ID);
        ReplaceSimilarBlocks(statsArray, Block.Farmland.ID, Block.Dirt.ID);
    }

    private static void ReplaceSimilarBlocks(StatBase[] statsArray, int sourceId, int targetId)
    {
        if (statsArray[sourceId] != null && statsArray[targetId] == null)
        {
            statsArray[targetId] = statsArray[sourceId];
        }
        else
        {
            AllStats.Remove(statsArray[sourceId]);
            BlocksMinedStats.Remove(statsArray[sourceId]);
            GeneralStats.Remove(statsArray[sourceId]);
            statsArray[sourceId] = statsArray[targetId];
        }
    }

    public static StatBase GetStatById(int id)
    {
        return IdToStat[id];
    }

    static Stats()
    {
        BetaSharp.Achievements.initialize();
    }
}
