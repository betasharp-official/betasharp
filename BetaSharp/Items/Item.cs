using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Items;

public class Item
{
    private readonly ILogger<Item> _logger = Log.Instance.For<Item>();

    static Item()
    {
        Stats.Stats.InitializeExtendedItemStats();
    }

    protected static JavaRandom itemRand = new();
    public static Item?[] ITEMS = new Item[32000];
    public static Item IronShovel = (new ItemSpade(0, EnumToolMaterial.IRON)).setTexturePosition(2, 5).setItemName("shovelIron");
    public static Item IronPickaxe = (new ItemPickaxe(1, EnumToolMaterial.IRON)).setTexturePosition(2, 6).setItemName("pickaxeIron");
    public static Item IronAxe = (new ItemAxe(2, EnumToolMaterial.IRON)).setTexturePosition(2, 7).setItemName("hatchetIron");
    public static Item FlintAndSteel = (new ItemFlintAndSteel(3)).setTexturePosition(5, 0).setItemName("flintAndSteel");
    public static Item Apple = (new ItemFood(4, 4, false)).setTexturePosition(10, 0).setItemName("apple");
    public static Item BOW = (new ItemBow(5)).setTexturePosition(5, 1).setItemName("bow");
    public static Item ARROW = (new Item(6)).setTexturePosition(5, 2).setItemName("arrow");
    public static Item Coal = (new ItemCoal(7)).setTexturePosition(7, 0).setItemName("coal");
    public static Item Diamond = (new Item(8)).setTexturePosition(7, 3).setItemName("emerald");
    public static Item IronIngot = (new Item(9)).setTexturePosition(7, 1).setItemName("ingotIron");
    public static Item GoldIngot = (new Item(10)).setTexturePosition(7, 2).setItemName("ingotGold");
    public static Item IronSword = (new ItemSword(11, EnumToolMaterial.IRON)).setTexturePosition(2, 4).setItemName("swordIron");
    public static Item WoodenSword = (new ItemSword(12, EnumToolMaterial.WOOD)).setTexturePosition(0, 4).setItemName("swordWood");
    public static Item WoodenShovel = (new ItemSpade(13, EnumToolMaterial.WOOD)).setTexturePosition(0, 5).setItemName("shovelWood");
    public static Item WoodenPickaxe = (new ItemPickaxe(14, EnumToolMaterial.WOOD)).setTexturePosition(0, 6).setItemName("pickaxeWood");
    public static Item WoodenAxe = (new ItemAxe(15, EnumToolMaterial.WOOD)).setTexturePosition(0, 7).setItemName("hatchetWood");
    public static Item StoneSword = (new ItemSword(16, EnumToolMaterial.STONE)).setTexturePosition(1, 4).setItemName("swordStone");
    public static Item StoneShovel = (new ItemSpade(17, EnumToolMaterial.STONE)).setTexturePosition(1, 5).setItemName("shovelStone");
    public static Item StonePickaxe = (new ItemPickaxe(18, EnumToolMaterial.STONE)).setTexturePosition(1, 6).setItemName("pickaxeStone");
    public static Item StoneAxe = (new ItemAxe(19, EnumToolMaterial.STONE)).setTexturePosition(1, 7).setItemName("hatchetStone");
    public static Item DiamondSword = (new ItemSword(20, EnumToolMaterial.EMERALD)).setTexturePosition(3, 4).setItemName("swordDiamond");
    public static Item DiamondShovel = (new ItemSpade(21, EnumToolMaterial.EMERALD)).setTexturePosition(3, 5).setItemName("shovelDiamond");
    public static Item DiamondPickaxe = (new ItemPickaxe(22, EnumToolMaterial.EMERALD)).setTexturePosition(3, 6).setItemName("pickaxeDiamond");
    public static Item DiamondAxe = (new ItemAxe(23, EnumToolMaterial.EMERALD)).setTexturePosition(3, 7).setItemName("hatchetDiamond");
    public static Item Stick = (new Item(24)).setTexturePosition(5, 3).setHandheld().setItemName("stick");
    public static Item Bowl = (new Item(25)).setTexturePosition(7, 4).setItemName("bowl");
    public static Item MushroomStew = (new ItemSoup(26, 10)).setTexturePosition(8, 4).setItemName("mushroomStew");
    public static Item GoldenSword = (new ItemSword(27, EnumToolMaterial.GOLD)).setTexturePosition(4, 4).setItemName("swordGold");
    public static Item GoldenShovel = (new ItemSpade(28, EnumToolMaterial.GOLD)).setTexturePosition(4, 5).setItemName("shovelGold");
    public static Item GoldenPickaxe = (new ItemPickaxe(29, EnumToolMaterial.GOLD)).setTexturePosition(4, 6).setItemName("pickaxeGold");
    public static Item GoldenAxe = (new ItemAxe(30, EnumToolMaterial.GOLD)).setTexturePosition(4, 7).setItemName("hatchetGold");
    public static Item String = (new Item(31)).setTexturePosition(8, 0).setItemName("string");
    public static Item Feather = (new Item(32)).setTexturePosition(8, 1).setItemName("feather");
    public static Item Gunpowder = (new Item(33)).setTexturePosition(8, 2).setItemName("sulphur");
    public static Item WoodenHoe = (new ItemHoe(34, EnumToolMaterial.WOOD)).setTexturePosition(0, 8).setItemName("hoeWood");
    public static Item StoneHoe = (new ItemHoe(35, EnumToolMaterial.STONE)).setTexturePosition(1, 8).setItemName("hoeStone");
    public static Item IronHoe = (new ItemHoe(36, EnumToolMaterial.IRON)).setTexturePosition(2, 8).setItemName("hoeIron");
    public static Item DiamondHoe = (new ItemHoe(37, EnumToolMaterial.EMERALD)).setTexturePosition(3, 8).setItemName("hoeDiamond");
    public static Item GoldenHoe = (new ItemHoe(38, EnumToolMaterial.GOLD)).setTexturePosition(4, 8).setItemName("hoeGold");
    public static Item Seeds = (new ItemSeeds(39, Block.Wheat.ID)).setTexturePosition(9, 0).setItemName("seeds");
    public static Item Wheat = (new Item(40)).setTexturePosition(9, 1).setItemName("wheat");
    public static Item Bread = (new ItemFood(41, 5, false)).setTexturePosition(9, 2).setItemName("bread");
    public static Item LeatherHelmet = (new ItemArmor(42, 0, 0, 0)).setTexturePosition(0, 0).setItemName("helmetCloth");
    public static Item LeatherChestplate = (new ItemArmor(43, 0, 0, 1)).setTexturePosition(0, 1).setItemName("chestplateCloth");
    public static Item LeatherLeggings = (new ItemArmor(44, 0, 0, 2)).setTexturePosition(0, 2).setItemName("leggingsCloth");
    public static Item LeatherBoots = (new ItemArmor(45, 0, 0, 3)).setTexturePosition(0, 3).setItemName("bootsCloth");
    public static Item ChainHelmet = (new ItemArmor(46, 1, 1, 0)).setTexturePosition(1, 0).setItemName("helmetChain");
    public static Item ChainChestplate = (new ItemArmor(47, 1, 1, 1)).setTexturePosition(1, 1).setItemName("chestplateChain");
    public static Item ChainLeggings = (new ItemArmor(48, 1, 1, 2)).setTexturePosition(1, 2).setItemName("leggingsChain");
    public static Item ChainBoots = (new ItemArmor(49, 1, 1, 3)).setTexturePosition(1, 3).setItemName("bootsChain");
    public static Item IronHelmet = (new ItemArmor(50, 2, 2, 0)).setTexturePosition(2, 0).setItemName("helmetIron");
    public static Item IronChestplate = (new ItemArmor(51, 2, 2, 1)).setTexturePosition(2, 1).setItemName("chestplateIron");
    public static Item IronLeggings = (new ItemArmor(52, 2, 2, 2)).setTexturePosition(2, 2).setItemName("leggingsIron");
    public static Item IronBoots = (new ItemArmor(53, 2, 2, 3)).setTexturePosition(2, 3).setItemName("bootsIron");
    public static Item DiamondHelmet = (new ItemArmor(54, 3, 3, 0)).setTexturePosition(3, 0).setItemName("helmetDiamond");
    public static Item DiamondChestplate = (new ItemArmor(55, 3, 3, 1)).setTexturePosition(3, 1).setItemName("chestplateDiamond");
    public static Item DiamondLeggings = (new ItemArmor(56, 3, 3, 2)).setTexturePosition(3, 2).setItemName("leggingsDiamond");
    public static Item DiamondBoots = (new ItemArmor(57, 3, 3, 3)).setTexturePosition(3, 3).setItemName("bootsDiamond");
    public static Item GoldenHelmet = (new ItemArmor(58, 1, 4, 0)).setTexturePosition(4, 0).setItemName("helmetGold");
    public static Item GoldenChestplate = (new ItemArmor(59, 1, 4, 1)).setTexturePosition(4, 1).setItemName("chestplateGold");
    public static Item GoldenLeggings = (new ItemArmor(60, 1, 4, 2)).setTexturePosition(4, 2).setItemName("leggingsGold");
    public static Item GoldenBoots = (new ItemArmor(61, 1, 4, 3)).setTexturePosition(4, 3).setItemName("bootsGold");
    public static Item Flint = (new Item(62)).setTexturePosition(6, 0).setItemName("flint");
    public static Item RawPorkchop = (new ItemFood(63, 3, true)).setTexturePosition(7, 5).setItemName("porkchopRaw");
    public static Item CookedPorkchop = (new ItemFood(64, 8, true)).setTexturePosition(8, 5).setItemName("porkchopCooked");
    public static Item Painting = (new ItemPainting(65)).setTexturePosition(10, 1).setItemName("painting");
    public static Item GoldenApple = (new ItemFood(66, 42, false)).setTexturePosition(11, 0).setItemName("appleGold");
    public static Item Sign = (new ItemSign(67)).setTexturePosition(10, 2).setItemName("sign");
    public static Item WoodenDoor = (new ItemDoor(68, Material.Wood)).setTexturePosition(11, 2).setItemName("doorWood");
    public static Item Bucket = (new ItemBucket(69, 0)).setTexturePosition(10, 4).setItemName("bucket");
    public static Item WaterBucket = (new ItemBucket(70, Block.FlowingWater.ID)).setTexturePosition(11, 4).setItemName("bucketWater").setCraftingReturnItem(Bucket);
    public static Item LavaBucket = (new ItemBucket(71, Block.FlowingLava.ID)).setTexturePosition(12, 4).setItemName("bucketLava").setCraftingReturnItem(Bucket);
    public static Item Minecart = (new ItemMinecart(72, 0)).setTexturePosition(7, 8).setItemName("minecart");
    public static Item Saddle = (new ItemSaddle(73)).setTexturePosition(8, 6).setItemName("saddle");
    public static Item IronDoor = (new ItemDoor(74, Material.Metal)).setTexturePosition(12, 2).setItemName("doorIron");
    public static Item Redstone = (new ItemRedstone(75)).setTexturePosition(8, 3).setItemName("redstone");
    public static Item Snowball = (new ItemSnowball(76)).setTexturePosition(14, 0).setItemName("snowball");
    public static Item Boat = (new ItemBoat(77)).setTexturePosition(8, 8).setItemName("boat");
    public static Item Leather = (new Item(78)).setTexturePosition(7, 6).setItemName("Leather");
    public static Item MilkBucket = (new ItemBucket(79, -1)).setTexturePosition(13, 4).setItemName("milk").setCraftingReturnItem(Bucket);
    public static Item Brick = (new Item(80)).setTexturePosition(6, 1).setItemName("brick");
    public static Item Clay = (new Item(81)).setTexturePosition(9, 3).setItemName("clay");
    public static Item SugarCane = (new ItemReed(82, Block.SugarCane)).setTexturePosition(11, 1).setItemName("reeds");
    public static Item Paper = (new Item(83)).setTexturePosition(10, 3).setItemName("paper");
    public static Item Book = (new Item(84)).setTexturePosition(11, 3).setItemName("book");
    public static Item Slimeball = (new Item(85)).setTexturePosition(14, 1).setItemName("slimeball");
    public static Item ChestMinecart = (new ItemMinecart(86, 1)).setTexturePosition(7, 9).setItemName("minecartChest");
    public static Item FurnaceMinecart = (new ItemMinecart(87, 2)).setTexturePosition(7, 10).setItemName("minecartFurnace");
    public static Item Egg = (new ItemEgg(88)).setTexturePosition(12, 0).setItemName("egg");
    public static Item Compass = (new Item(89)).setTexturePosition(6, 3).setItemName("compass");
    public static Item FishingRod = (new ItemFishingRod(90)).setTexturePosition(5, 4).setItemName("fishingRod");
    public static Item Clock = (new Item(91)).setTexturePosition(6, 4).setItemName("clock");
    public static Item GlowstoneDust = (new Item(92)).setTexturePosition(9, 4).setItemName("yellowDust");
    public static Item RawFish = (new ItemFood(93, 2, false)).setTexturePosition(9, 5).setItemName("fishRaw");
    public static Item CookedFish = (new ItemFood(94, 5, false)).setTexturePosition(10, 5).setItemName("fishCooked");
    public static Item Dye = (new ItemDye(95)).setTexturePosition(14, 4).setItemName("dyePowder");
    public static Item Bone = (new Item(96)).setTexturePosition(12, 1).setItemName("bone").setHandheld();
    public static Item Sugar = (new Item(97)).setTexturePosition(13, 0).setItemName("sugar").setHandheld();
    public static Item Cake = (new ItemReed(98, Block.Cake)).setMaxCount(1).setTexturePosition(13, 1).setItemName("cake");
    public static Item Bed = (new ItemBed(99)).setMaxCount(1).setTexturePosition(13, 2).setItemName("bed");
    public static Item Repeater = (new ItemReed(100, Block.Repeater)).setTexturePosition(6, 5).setItemName("diode");
    public static Item Cookie = (new ItemCookie(101, 1, false, 8)).setTexturePosition(12, 5).setItemName("cookie");
    public static ItemMap Map = (ItemMap)(new ItemMap(102)).setTexturePosition(12, 3).setItemName("map");
    public static ItemShears Shears = (ItemShears)(new ItemShears(103)).setTexturePosition(13, 5).setItemName("shears");
    public static Item RecordThirteen = (new ItemRecord(2000, "13")).setTexturePosition(0, 15).setItemName("record");
    public static Item RecordCat = (new ItemRecord(2001, "cat")).setTexturePosition(1, 15).setItemName("record");
    public readonly int id;
    public int maxCount = 64;
    private int maxDamage;
    protected int textureId;
    protected bool handheld;
    protected bool hasSubtypes;
    private Item craftingReturnItem;
    private string translationKey;

    protected Item(int id)
    {
        this.id = 256 + id;
        if (ITEMS[256 + id] != null)
        {
            _logger.LogInformation($"CONFLICT @ {id}");
        }

        ITEMS[256 + id] = this;
    }

    public Item setTextureId(int textureId)
    {
        this.textureId = textureId;
        return this;
    }

    public Item setMaxCount(int maxCount)
    {
        this.maxCount = maxCount;
        return this;
    }

    public Item setTexturePosition(int x, int y)
    {
        textureId = x + y * 16;
        return this;
    }

    public virtual int getTextureId(int damage)
    {
        return textureId;
    }

    public int getTextureId(ItemStack stack)
    {
        return getTextureId(stack.getDamage());
    }

    public virtual bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        return false;
    }

    public virtual float getMiningSpeedMultiplier(ItemStack itemStack, Block block)
    {
        return 1.0F;
    }

    public virtual ItemStack use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        return itemStack;
    }

    public int getMaxCount()
    {
        return maxCount;
    }

    public virtual int getPlacementMetadata(int meta)
    {
        return 0;
    }

    public bool getHasSubtypes()
    {
        return hasSubtypes;
    }

    protected Item setHasSubtypes(bool has)
    {
        hasSubtypes = has;
        return this;
    }

    public int getMaxDamage()
    {
        return maxDamage;
    }

    protected Item setMaxDamage(int dmg)
    {
        maxDamage = dmg;
        return this;
    }

    public bool isDamagable()
    {
        return maxDamage > 0 && !hasSubtypes;
    }

    public virtual bool postHit(ItemStack itemStack, EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
        return false;
    }

    public virtual bool postMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving)
    {
        return false;
    }

    public virtual int getAttackDamage(Entity entity)
    {
        return 1;
    }

    public virtual bool isSuitableFor(Block block)
    {
        return false;
    }

    public virtual void useOnEntity(ItemStack itemStack, EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
    }

    public Item setHandheld()
    {
        handheld = true;
        return this;
    }

    public virtual bool isHandheld()
    {
        return handheld;
    }

    public virtual bool isHandheldRod()
    {
        return false;
    }

    public Item setItemName(string name)
    {
        translationKey = "item." + name;
        return this;
    }

    public virtual string getItemName()
    {
        return translationKey;
    }

    public virtual string getItemNameIS(ItemStack itemStack)
    {
        return translationKey;
    }

    public Item setCraftingReturnItem(Item item)
    {
        if (maxCount > 1)
        {
            throw new ArgumentException("Max stack size must be 1 for items with crafting results");
        }
        else
        {
            craftingReturnItem = item;
            return this;
        }
    }

    public Item getContainerItem()
    {
        return craftingReturnItem;
    }

    public bool hasContainerItem()
    {
        return craftingReturnItem != null;
    }

    public string getStatName()
    {
        return StatCollector.TranslateToLocal(getItemName() + ".name");
    }

    public virtual int getColorMultiplier(int color)
    {
        return 0xFFFFFF;
    }

    public virtual void inventoryTick(ItemStack itemStack, IWorldContext world, Entity entity, int slotIndex, bool shouldUpdate)
    {
    }

    public virtual void onCraft(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
    }

    public virtual bool isNetworkSynced()
    {
        return false;
    }
}
