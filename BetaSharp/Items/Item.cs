using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Items;

public class Item
{
    protected static JavaRandom itemRand = new();
    public static Item?[] ITEMS = new Item[32000];
    public static Item IronShovel = new ItemSpade(0, ToolMaterial.IRON).SetTexturePosition(2, 5).SetItemName("shovelIron");
    public static Item IronPickaxe = new ItemPickaxe(1, ToolMaterial.IRON).SetTexturePosition(2, 6).SetItemName("pickaxeIron");
    public static Item IronAxe = new ItemAxe(2, ToolMaterial.IRON).SetTexturePosition(2, 7).SetItemName("hatchetIron");
    public static Item FlintAndSteel = new ItemFlintAndSteel(3).SetTexturePosition(5, 0).SetItemName("flintAndSteel");
    public static Item Apple = new ItemFood(4, 4, false).SetTexturePosition(10, 0).SetItemName("apple");
    public static Item BOW = new ItemBow(5).SetTexturePosition(5, 1).SetItemName("bow");
    public static Item ARROW = new Item(6).SetTexturePosition(5, 2).SetItemName("arrow");
    public static Item Coal = new ItemCoal(7).SetTexturePosition(7, 0).SetItemName("coal");
    public static Item Diamond = new Item(8).SetTexturePosition(7, 3).SetItemName("emerald");
    public static Item IronIngot = new Item(9).SetTexturePosition(7, 1).SetItemName("ingotIron");
    public static Item GoldIngot = new Item(10).SetTexturePosition(7, 2).SetItemName("ingotGold");
    public static Item IronSword = new ItemSword(11, ToolMaterial.IRON).SetTexturePosition(2, 4).SetItemName("swordIron");
    public static Item WoodenSword = new ItemSword(12, ToolMaterial.WOOD).SetTexturePosition(0, 4).SetItemName("swordWood");
    public static Item WoodenShovel = new ItemSpade(13, ToolMaterial.WOOD).SetTexturePosition(0, 5).SetItemName("shovelWood");
    public static Item WoodenPickaxe = new ItemPickaxe(14, ToolMaterial.WOOD).SetTexturePosition(0, 6).SetItemName("pickaxeWood");
    public static Item WoodenAxe = new ItemAxe(15, ToolMaterial.WOOD).SetTexturePosition(0, 7).SetItemName("hatchetWood");
    public static Item StoneSword = new ItemSword(16, ToolMaterial.STONE).SetTexturePosition(1, 4).SetItemName("swordStone");
    public static Item StoneShovel = new ItemSpade(17, ToolMaterial.STONE).SetTexturePosition(1, 5).SetItemName("shovelStone");
    public static Item StonePickaxe = new ItemPickaxe(18, ToolMaterial.STONE).SetTexturePosition(1, 6).SetItemName("pickaxeStone");
    public static Item StoneAxe = new ItemAxe(19, ToolMaterial.STONE).SetTexturePosition(1, 7).SetItemName("hatchetStone");
    public static Item DiamondSword = new ItemSword(20, ToolMaterial.EMERALD).SetTexturePosition(3, 4).SetItemName("swordDiamond");
    public static Item DiamondShovel = new ItemSpade(21, ToolMaterial.EMERALD).SetTexturePosition(3, 5).SetItemName("shovelDiamond");
    public static Item DiamondPickaxe = new ItemPickaxe(22, ToolMaterial.EMERALD).SetTexturePosition(3, 6).SetItemName("pickaxeDiamond");
    public static Item DiamondAxe = new ItemAxe(23, ToolMaterial.EMERALD).SetTexturePosition(3, 7).SetItemName("hatchetDiamond");
    public static Item Stick = new Item(24).SetTexturePosition(5, 3).SetHandheld().SetItemName("stick");
    public static Item Bowl = new Item(25).SetTexturePosition(7, 4).SetItemName("bowl");
    public static Item MushroomStew = new ItemSoup(26, 10).SetTexturePosition(8, 4).SetItemName("mushroomStew");
    public static Item GoldenSword = new ItemSword(27, ToolMaterial.GOLD).SetTexturePosition(4, 4).SetItemName("swordGold");
    public static Item GoldenShovel = new ItemSpade(28, ToolMaterial.GOLD).SetTexturePosition(4, 5).SetItemName("shovelGold");
    public static Item GoldenPickaxe = new ItemPickaxe(29, ToolMaterial.GOLD).SetTexturePosition(4, 6).SetItemName("pickaxeGold");
    public static Item GoldenAxe = new ItemAxe(30, ToolMaterial.GOLD).SetTexturePosition(4, 7).SetItemName("hatchetGold");
    public static Item String = new Item(31).SetTexturePosition(8, 0).SetItemName("string");
    public static Item Feather = new Item(32).SetTexturePosition(8, 1).SetItemName("feather");
    public static Item Gunpowder = new Item(33).SetTexturePosition(8, 2).SetItemName("sulphur");
    public static Item WoodenHoe = new ItemHoe(34, ToolMaterial.WOOD).SetTexturePosition(0, 8).SetItemName("hoeWood");
    public static Item StoneHoe = new ItemHoe(35, ToolMaterial.STONE).SetTexturePosition(1, 8).SetItemName("hoeStone");
    public static Item IronHoe = new ItemHoe(36, ToolMaterial.IRON).SetTexturePosition(2, 8).SetItemName("hoeIron");
    public static Item DiamondHoe = new ItemHoe(37, ToolMaterial.EMERALD).SetTexturePosition(3, 8).SetItemName("hoeDiamond");
    public static Item GoldenHoe = new ItemHoe(38, ToolMaterial.GOLD).SetTexturePosition(4, 8).SetItemName("hoeGold");
    public static Item Seeds = new ItemSeeds(39, Block.Wheat.id).SetTexturePosition(9, 0).SetItemName("seeds");
    public static Item Wheat = new Item(40).SetTexturePosition(9, 1).SetItemName("wheat");
    public static Item Bread = new ItemFood(41, 5, false).SetTexturePosition(9, 2).SetItemName("bread");
    public static Item LeatherHelmet = new ItemArmor(42, 0, 0, 0).SetTexturePosition(0, 0).SetItemName("helmetCloth");
    public static Item LeatherChestplate = new ItemArmor(43, 0, 0, 1).SetTexturePosition(0, 1).SetItemName("chestplateCloth");
    public static Item LeatherLeggings = new ItemArmor(44, 0, 0, 2).SetTexturePosition(0, 2).SetItemName("leggingsCloth");
    public static Item LeatherBoots = new ItemArmor(45, 0, 0, 3).SetTexturePosition(0, 3).SetItemName("bootsCloth");
    public static Item ChainHelmet = new ItemArmor(46, 1, 1, 0).SetTexturePosition(1, 0).SetItemName("helmetChain");
    public static Item ChainChestplate = new ItemArmor(47, 1, 1, 1).SetTexturePosition(1, 1).SetItemName("chestplateChain");
    public static Item ChainLeggings = new ItemArmor(48, 1, 1, 2).SetTexturePosition(1, 2).SetItemName("leggingsChain");
    public static Item ChainBoots = new ItemArmor(49, 1, 1, 3).SetTexturePosition(1, 3).SetItemName("bootsChain");
    public static Item IronHelmet = new ItemArmor(50, 2, 2, 0).SetTexturePosition(2, 0).SetItemName("helmetIron");
    public static Item IronChestplate = new ItemArmor(51, 2, 2, 1).SetTexturePosition(2, 1).SetItemName("chestplateIron");
    public static Item IronLeggings = new ItemArmor(52, 2, 2, 2).SetTexturePosition(2, 2).SetItemName("leggingsIron");
    public static Item IronBoots = new ItemArmor(53, 2, 2, 3).SetTexturePosition(2, 3).SetItemName("bootsIron");
    public static Item DiamondHelmet = new ItemArmor(54, 3, 3, 0).SetTexturePosition(3, 0).SetItemName("helmetDiamond");
    public static Item DiamondChestplate = new ItemArmor(55, 3, 3, 1).SetTexturePosition(3, 1).SetItemName("chestplateDiamond");
    public static Item DiamondLeggings = new ItemArmor(56, 3, 3, 2).SetTexturePosition(3, 2).SetItemName("leggingsDiamond");
    public static Item DiamondBoots = new ItemArmor(57, 3, 3, 3).SetTexturePosition(3, 3).SetItemName("bootsDiamond");
    public static Item GoldenHelmet = new ItemArmor(58, 1, 4, 0).SetTexturePosition(4, 0).SetItemName("helmetGold");
    public static Item GoldenChestplate = new ItemArmor(59, 1, 4, 1).SetTexturePosition(4, 1).SetItemName("chestplateGold");
    public static Item GoldenLeggings = new ItemArmor(60, 1, 4, 2).SetTexturePosition(4, 2).SetItemName("leggingsGold");
    public static Item GoldenBoots = new ItemArmor(61, 1, 4, 3).SetTexturePosition(4, 3).SetItemName("bootsGold");
    public static Item Flint = new Item(62).SetTexturePosition(6, 0).SetItemName("flint");
    public static Item RawPorkchop = new ItemFood(63, 3, true).SetTexturePosition(7, 5).SetItemName("porkchopRaw");
    public static Item CookedPorkchop = new ItemFood(64, 8, true).SetTexturePosition(8, 5).SetItemName("porkchopCooked");
    public static Item Painting = new ItemPainting(65).SetTexturePosition(10, 1).SetItemName("painting");
    public static Item GoldenApple = new ItemFood(66, 42, false).SetTexturePosition(11, 0).SetItemName("appleGold");
    public static Item Sign = new ItemSign(67).SetTexturePosition(10, 2).SetItemName("sign");
    public static Item WoodenDoor = new ItemDoor(68, Material.Wood).SetTexturePosition(11, 2).SetItemName("doorWood");
    public static Item Bucket = new ItemBucket(69, 0).SetTexturePosition(10, 4).SetItemName("bucket");
    public static Item WaterBucket = new ItemBucket(70, Block.FlowingWater.id).SetTexturePosition(11, 4).SetItemName("bucketWater").SetCraftingReturnItem(Bucket);
    public static Item LavaBucket = new ItemBucket(71, Block.FlowingLava.id).SetTexturePosition(12, 4).SetItemName("bucketLava").SetCraftingReturnItem(Bucket);
    public static Item Minecart = new ItemMinecart(72, 0).SetTexturePosition(7, 8).SetItemName("minecart");
    public static Item Saddle = new ItemSaddle(73).SetTexturePosition(8, 6).SetItemName("saddle");
    public static Item IronDoor = new ItemDoor(74, Material.Metal).SetTexturePosition(12, 2).SetItemName("doorIron");
    public static Item Redstone = new ItemRedstone(75).SetTexturePosition(8, 3).SetItemName("redstone");
    public static Item Snowball = new ItemSnowball(76).SetTexturePosition(14, 0).SetItemName("snowball");
    public static Item Boat = new ItemBoat(77).SetTexturePosition(8, 8).SetItemName("boat");
    public static Item Leather = new Item(78).SetTexturePosition(7, 6).SetItemName("Leather");
    public static Item MilkBucket = new ItemBucket(79, -1).SetTexturePosition(13, 4).SetItemName("milk").SetCraftingReturnItem(Bucket);
    public static Item Brick = new Item(80).SetTexturePosition(6, 1).SetItemName("brick");
    public static Item Clay = new Item(81).SetTexturePosition(9, 3).SetItemName("clay");
    public static Item SugarCane = new ItemReed(82, Block.SugarCane).SetTexturePosition(11, 1).SetItemName("reeds");
    public static Item Paper = new Item(83).SetTexturePosition(10, 3).SetItemName("paper");
    public static Item Book = new Item(84).SetTexturePosition(11, 3).SetItemName("book");
    public static Item Slimeball = new Item(85).SetTexturePosition(14, 1).SetItemName("slimeball");
    public static Item ChestMinecart = new ItemMinecart(86, 1).SetTexturePosition(7, 9).SetItemName("minecartChest");
    public static Item FurnaceMinecart = new ItemMinecart(87, 2).SetTexturePosition(7, 10).SetItemName("minecartFurnace");
    public static Item Egg = new ItemEgg(88).SetTexturePosition(12, 0).SetItemName("egg");
    public static Item Compass = new Item(89).SetTexturePosition(6, 3).SetItemName("compass");
    public static Item FishingRod = new ItemFishingRod(90).SetTexturePosition(5, 4).SetItemName("fishingRod");
    public static Item Clock = new Item(91).SetTexturePosition(6, 4).SetItemName("clock");
    public static Item GlowstoneDust = new Item(92).SetTexturePosition(9, 4).SetItemName("yellowDust");
    public static Item RawFish = new ItemFood(93, 2, false).SetTexturePosition(9, 5).SetItemName("fishRaw");
    public static Item CookedFish = new ItemFood(94, 5, false).SetTexturePosition(10, 5).SetItemName("fishCooked");
    public static Item Dye = new ItemDye(95).SetTexturePosition(14, 4).SetItemName("dyePowder");
    public static Item Bone = new Item(96).SetTexturePosition(12, 1).SetItemName("bone").SetHandheld();
    public static Item Sugar = new Item(97).SetTexturePosition(13, 0).SetItemName("sugar").SetHandheld();
    public static Item Cake = new ItemReed(98, Block.Cake).SetMaxCount(1).SetTexturePosition(13, 1).SetItemName("cake");
    public static Item Bed = new ItemBed(99).SetMaxCount(1).SetTexturePosition(13, 2).SetItemName("bed");
    public static Item Repeater = new ItemReed(100, Block.Repeater).SetTexturePosition(6, 5).SetItemName("diode");
    public static Item Cookie = new ItemCookie(101, 1, false, 8).SetTexturePosition(12, 5).SetItemName("cookie");
    public static ItemMap Map = (ItemMap)new ItemMap(102).SetTexturePosition(12, 3).SetItemName("map");
    public static ItemShears Shears = (ItemShears)new ItemShears(103).SetTexturePosition(13, 5).SetItemName("shears");
    public static Item RecordThirteen = new ItemRecord(2000, "13").SetTexturePosition(0, 15).SetItemName("record");
    public static Item RecordCat = new ItemRecord(2001, "cat").SetTexturePosition(1, 15).SetItemName("record");
    private readonly ILogger<Item> _logger = Log.Instance.For<Item>();
    public readonly int Id;
    private Item _craftingReturnItem;
    public bool Handheld;
    public bool HasSubtypes;
    protected int MaxCount = 64;
    private int _maxDamage;
    protected int TextureId;
    private string _translationKey;

    static Item() => Stats.Stats.InitializeExtendedItemStats();

    protected Item(int id)
    {
        Id = 256 + id;
        if (ITEMS[256 + id] != null)
        {
            _logger.LogInformation($"CONFLICT @ {id}");
        }

        ITEMS[256 + id] = this;
    }

    public Item SetTextureId(int textureId)
    {
        TextureId = textureId;
        return this;
    }

    public Item SetMaxCount(int maxCount)
    {
        MaxCount = maxCount;
        return this;
    }

    public Item SetTexturePosition(int x, int y)
    {
        TextureId = x + y * 16;
        return this;
    }

    public virtual int GetTextureId(int damage) => TextureId;

    public int GetTextureId(ItemStack stack) => GetTextureId(stack.GetDamage());

    public virtual bool UseOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta) => false;

    public virtual float GetMiningSpeedMultiplier(ItemStack itemStack, Block block) => 1.0F;

    public virtual ItemStack Use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer) => itemStack;

    public int GetMaxCount() => MaxCount;

    protected virtual int GetPlacementMetadata(int meta) => 0;

    public bool GetHasSubtypes() => HasSubtypes;

    protected Item SetHasSubtypes(bool has)
    {
        HasSubtypes = has;
        return this;
    }

    public int GetMaxDamage() => _maxDamage;

    protected Item SetMaxDamage(int dmg)
    {
        _maxDamage = dmg;
        return this;
    }

    public bool IsDamagable() => _maxDamage > 0 && !HasSubtypes;

    public virtual bool PostHit(ItemStack itemStack, EntityLiving entityLiving, EntityPlayer entityPlayer) => false;

    public virtual bool PostMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving) => false;

    public virtual int GetAttackDamage(Entity entity) => 1;

    public virtual bool IsSuitableFor(Block block) => false;

    public virtual void UseOnEntity(ItemStack itemStack, EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
    }

    public Item SetHandheld()
    {
        Handheld = true;
        return this;
    }

    public virtual bool IsHandheld() => Handheld;

    public virtual bool IsHandheldRod() => false;

    public Item SetItemName(string name)
    {
        _translationKey = "item." + name;
        return this;
    }

    public virtual string GetItemName() => _translationKey;

    public virtual string GetItemNameIS(ItemStack itemStack) => _translationKey;

    public Item SetCraftingReturnItem(Item item)
    {
        if (MaxCount > 1)
        {
            throw new ArgumentException("Max stack size must be 1 for items with crafting results");
        }

        _craftingReturnItem = item;
        return this;
    }

    public Item GetContainerItem() => _craftingReturnItem;

    public bool HasContainerItem() => _craftingReturnItem != null;

    public string GetStatName() => StatCollector.TranslateToLocal(GetItemName() + ".name");

    public virtual int GetColorMultiplier(int color) => 0xFFFFFF;

    public virtual void InventoryTick(ItemStack itemStack, IWorldContext world, Entity entity, int slotIndex, bool shouldUpdate)
    {
    }

    public virtual void OnCraft(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
    }

    public virtual bool IsNetworkSynced() => false;
}
