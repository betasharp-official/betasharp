using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Rules;
using BetaSharp.Stats;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class Block
{
    public static readonly BlockSoundGroup SoundPowderFootstep = new("stone", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundWoodFootstep = new("wood", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundGravelFootstep = new("gravel", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundGrassFootstep = new("grass", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundStoneFootstep = new("stone", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundMetalFootstep = new("stone", 1.0F, 1.5F);
    public static readonly BlockSoundGroup SoundGlassFootstep = new("stone", 1.0F, 1.0F, "random.glass");
    public static readonly BlockSoundGroup SoundClothFootstep = new("cloth", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundSandFootstep = new("sand", 1.0F, 1.0F, "step.gravel");

    public static readonly Block[] Blocks = new Block[256];
    public static readonly bool[] BlocksRandomTick = new bool[256];
    public static readonly bool[] BlocksOpaque = new bool[256];
    public static readonly bool[] BlocksWithEntity = new bool[256];
    public static readonly int[] BlockLightOpacity = new int[256];
    public static readonly bool[] BlocksAllowVision = new bool[256];
    public static readonly int[] BlocksLightLuminance = new int[256];
    public static readonly bool[] BlocksIgnoreMetaUpdate = new bool[256];

    public static readonly Block Stone = new BlockStone(1, BlockTextures.Stone).setHardness(1.5F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stone");
    public static readonly BlockGrass GrassBlock = (BlockGrass)new BlockGrass(2).setHardness(0.6F).setSoundGroup(SoundGrassFootstep).setBlockName("grass").SetVariance(TextureVariance.Rotations, TextureVariance.All, TextureVariance.None);
    public static readonly Block Dirt = new BlockDirt(3).setHardness(0.5F).setSoundGroup(SoundGravelFootstep).setBlockName("dirt").SetVariance(TextureVariance.All);
    public static readonly Block Cobblestone = new BlockCobblestone(4).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stonebrick");
    public static readonly Block Planks = new Block(5, BlockTextures.OakPlanks, Material.Wood).setHardness(2.0F).setResistance(5.0F).setSoundGroup(SoundWoodFootstep).setBlockName("wood").IgnoreMetaUpdates();
    public static readonly Block Sapling = new BlockSapling(6).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("sapling").IgnoreMetaUpdates();

    public static readonly Block Bedrock = new Block(7, BlockTextures.Bedrock, Material.Stone).setUnbreakable().setResistance(6000000.0F).setSoundGroup(SoundStoneFootstep).setBlockName("bedrock").disableStats()
        .SetVariance(TextureVariance.All);

    public static readonly Block FlowingWater = new BlockFlowing(8, Material.Water).setHardness(100.0F).setOpacity(3).setBlockName("water").disableStats().IgnoreMetaUpdates();
    public static readonly Block Water = new BlockStationary(9, Material.Water).setHardness(100.0F).setOpacity(3).setBlockName("water").disableStats().IgnoreMetaUpdates();
    public static readonly Block FlowingLava = new BlockFlowing(10, Material.Lava).setHardness(0.0F).setLuminance(1.0F).setOpacity(255).setBlockName("lava").disableStats().IgnoreMetaUpdates();
    public static readonly Block Lava = new BlockStationary(11, Material.Lava).setHardness(100.0F).setLuminance(1.0F).setOpacity(255).setBlockName("lava").disableStats().IgnoreMetaUpdates();
    public static readonly Block Sand = new BlockSand(12, BlockTextures.Sand).setHardness(0.5F).setSoundGroup(SoundSandFootstep).setBlockName("sand").SetVariance(TextureVariance.Rotations);
    public static readonly Block Gravel = new BlockGravel(13, BlockTextures.Gravel).setHardness(0.6F).setSoundGroup(SoundGravelFootstep).setBlockName("gravel").SetVariance(TextureVariance.Rotations);

    public static readonly Block GoldOre = new BlockOre(14, BlockTextures.GoldOre).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreGold")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block IronOre = new BlockOre(15, BlockTextures.IronOre).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreIron")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block CoalOre = new BlockOre(16, BlockTextures.CoalOre).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreCoal")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block Log = new BlockLog(17).setHardness(2.0F).setSoundGroup(SoundWoodFootstep).setBlockName("log").IgnoreMetaUpdates().SetVariance(TextureVariance.All, TextureVariance.Rotate180);

    public static readonly BlockLeaves Leaves = (BlockLeaves)new BlockLeaves(18, BlockTextures.LeavesOak).setHardness(0.2F).setOpacity(1).setSoundGroup(SoundGrassFootstep).setBlockName("leaves").disableStats().IgnoreMetaUpdates()
        .SetVariance(TextureVariance.All, TextureVariance.Rotate180);

    public static readonly Block Sponge = new BlockSponge(19).setHardness(0.6F).setSoundGroup(SoundGrassFootstep).setBlockName("sponge").SetVariance(TextureVariance.All, TextureVariance.FlipBoth);
    public static readonly Block Glass = new BlockGlass(20, BlockTextures.Glass, Material.Glass, false).setHardness(0.3F).setSoundGroup(SoundGlassFootstep).setBlockName("glass").SetVariance(TextureVariance.Rotate180);

    public static readonly Block LapisOre = new BlockOre(21, BlockTextures.LapisOre).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreLapis")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block LapisBlock = new Block(22, BlockTextures.BlockLapis, Material.Stone).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("blockLapis");
    public static readonly Block Dispenser = new BlockDispenser(23).setHardness(3.5F).setSoundGroup(SoundStoneFootstep).setBlockName("dispenser").IgnoreMetaUpdates();
    public static readonly Block Sandstone = new BlockSandStone(24).setSoundGroup(SoundStoneFootstep).setHardness(0.8F).setBlockName("sandStone").SetVariance(TextureVariance.Rotations, TextureVariance.None);
    public static readonly Block Noteblock = new BlockNote(25).setHardness(0.8F).setBlockName("musicBlock").IgnoreMetaUpdates();
    public static readonly Block Bed = new BlockBed(26).setHardness(0.2F).setBlockName("bed").disableStats().IgnoreMetaUpdates();
    public static readonly Block PoweredRail = new BlockRail(27, BlockTextures.PoweredRailOn, true).setHardness(0.7F).setSoundGroup(SoundMetalFootstep).setBlockName("goldenRail").IgnoreMetaUpdates();
    public static readonly Block DetectorRail = new BlockDetectorRail(28, BlockTextures.DetectorRail).setHardness(0.7F).setSoundGroup(SoundMetalFootstep).setBlockName("detectorRail").IgnoreMetaUpdates();
    public static readonly Block StickyPiston = new BlockPistonBase(29, BlockTextures.PistonTopSticky, true).setBlockName("pistonStickyBase").IgnoreMetaUpdates();
    public static readonly Block Cobweb = new BlockWeb(30, BlockTextures.Cobweb).setOpacity(1).setHardness(4.0F).setBlockName("web");
    public static readonly BlockTallGrass Grass = (BlockTallGrass)new BlockTallGrass(31, BlockTextures.TallGrass).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("tallgrass");
    public static readonly BlockDeadBush DeadBush = (BlockDeadBush)new BlockDeadBush(32, BlockTextures.DeadBush).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("deadbush");
    public static readonly Block Piston = new BlockPistonBase(33, BlockTextures.PistonTopNormal, false).setBlockName("pistonBase").IgnoreMetaUpdates();
    public static readonly BlockPistonExtension PistonHead = (BlockPistonExtension)new BlockPistonExtension(34, BlockTextures.PistonTopNormal).IgnoreMetaUpdates();
    public static readonly Block Wool = new BlockCloth().setHardness(0.8F).setSoundGroup(SoundClothFootstep).setBlockName("cloth").IgnoreMetaUpdates().SetVariance(TextureVariance.None, TextureVariance.FlipBoth);
    public static readonly BlockPistonMoving MovingPiston = new(36);
    public static readonly BlockPlant Dandelion = (BlockPlant)new BlockPlant(37, BlockTextures.Dandelion).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("flower");
    public static readonly BlockPlant Rose = (BlockPlant)new BlockPlant(38, BlockTextures.Rose).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("rose");
    public static readonly BlockPlant BrownMushroom = (BlockPlant)new BlockMushroom(39, BlockTextures.BrownMushroom).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setLuminance(2.0F / 16.0F).setBlockName("mushroom");
    public static readonly BlockPlant RedMushroom = (BlockPlant)new BlockMushroom(40, BlockTextures.RedMushroom).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("mushroom");
    public static readonly Block GoldBlock = new BlockOreStorage(41, BlockTextures.BlockGold).setHardness(3.0F).setResistance(10.0F).setSoundGroup(SoundMetalFootstep).setBlockName("blockGold");
    public static readonly Block IronBlock = new BlockOreStorage(42, BlockTextures.BlockIron).setHardness(5.0F).setResistance(10.0F).setSoundGroup(SoundMetalFootstep).setBlockName("blockIron");
    public static readonly Block DoubleSlab = new BlockSlab(43, true).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stoneSlab");
    public static readonly Block Slab = new BlockSlab(44, false).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stoneSlab");
    public static readonly Block Bricks = new Block(45, BlockTextures.Bricks, Material.Stone).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("brick");
    public static readonly Block TNT = new BlockTNT(46, BlockTextures.TntSide).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("tnt");
    public static readonly Block Bookshelf = new BlockBookshelf(47, BlockTextures.Bookshelf).setHardness(1.5F).setSoundGroup(SoundWoodFootstep).setBlockName("bookshelf").SetVariance(TextureVariance.None, TextureVariance.FlipU);
    public static readonly Block MossyCobblestone = new Block(48, BlockTextures.MossyCobblestone, Material.Stone).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stoneMoss");
    public static readonly Block Obsidian = new BlockObsidian(49, BlockTextures.Obsidian).setHardness(10.0F).setResistance(2000.0F).setSoundGroup(SoundStoneFootstep).setBlockName("obsidian");
    public static readonly Block Torch = new BlockTorch(50, BlockTextures.Torch).setHardness(0.0F).setLuminance(15.0F / 16.0F).setSoundGroup(SoundWoodFootstep).setBlockName("torch").IgnoreMetaUpdates();
    public static readonly Block Fire = (BlockFire)new BlockFire(51, BlockTextures.Fire).setHardness(0.0F).setLuminance(1.0F).setSoundGroup(SoundWoodFootstep).setBlockName("fire").disableStats().IgnoreMetaUpdates();
    public static readonly Block Spawner = new BlockMobSpawner(52, BlockTextures.Spawner).setHardness(5.0F).setSoundGroup(SoundMetalFootstep).setBlockName("mobSpawner").disableStats();
    public static readonly Block WoodenStairs = new BlockStairs(53, Planks).setBlockName("stairsWood").IgnoreMetaUpdates();
    public static readonly Block Chest = new BlockChest(54).setHardness(2.5F).setSoundGroup(SoundWoodFootstep).setBlockName("chest").IgnoreMetaUpdates();
    public static readonly Block RedstoneWire = new BlockRedstoneWire(55, BlockTextures.RedstoneWireCross).setHardness(0.0F).setSoundGroup(SoundPowderFootstep).setBlockName("redstoneDust").disableStats().IgnoreMetaUpdates();

    public static readonly Block DiamondOre = new BlockOre(56, BlockTextures.DiamondOre).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreDiamond")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block DiamondBlock = new BlockOreStorage(57, BlockTextures.BlockDiamond).setHardness(5.0F).setResistance(10.0F).setSoundGroup(SoundMetalFootstep).setBlockName("blockDiamond");
    public static readonly Block CraftingTable = new BlockWorkbench(58).setHardness(2.5F).setSoundGroup(SoundWoodFootstep).setBlockName("workbench");
    public static readonly Block Wheat = new BlockCrops(59, BlockTextures.WheatStageBase).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("crops").disableStats().IgnoreMetaUpdates();
    public static readonly Block Farmland = new BlockFarmland(60).setHardness(0.6F).setSoundGroup(SoundGravelFootstep).setBlockName("farmland");
    public static readonly Block Furnace = new BlockFurnace(61, false).setHardness(3.5F).setSoundGroup(SoundStoneFootstep).setBlockName("furnace").IgnoreMetaUpdates();
    public static readonly Block LitFurnace = new BlockFurnace(62, true).setHardness(3.5F).setSoundGroup(SoundStoneFootstep).setLuminance(14.0F / 16.0F).setBlockName("furnace").IgnoreMetaUpdates();
    public static readonly Block Sign = new BlockSign(63, typeof(BlockEntitySign), true).setHardness(1.0F).setSoundGroup(SoundWoodFootstep).setBlockName("sign").disableStats().IgnoreMetaUpdates();
    public static readonly Block Door = new BlockDoor(64, Material.Wood).setHardness(3.0F).setSoundGroup(SoundWoodFootstep).setBlockName("doorWood").disableStats().IgnoreMetaUpdates();
    public static readonly Block Ladder = new BlockLadder(65, BlockTextures.Ladder).setHardness(0.4F).setSoundGroup(SoundWoodFootstep).setBlockName("ladder").IgnoreMetaUpdates();
    public static readonly Block Rail = new BlockRail(66, BlockTextures.RailStraight, false).setHardness(0.7F).setSoundGroup(SoundMetalFootstep).setBlockName("rail").IgnoreMetaUpdates();
    public static readonly Block CobblestoneStairs = new BlockStairs(67, Cobblestone).setBlockName("stairsStone").IgnoreMetaUpdates();
    public static readonly Block WallSign = new BlockSign(68, typeof(BlockEntitySign), false).setHardness(1.0F).setSoundGroup(SoundWoodFootstep).setBlockName("sign").disableStats().IgnoreMetaUpdates();
    public static readonly Block Lever = new BlockLever(69, BlockTextures.Lever).setHardness(0.5F).setSoundGroup(SoundWoodFootstep).setBlockName("lever").IgnoreMetaUpdates();

    public static readonly Block StonePressurePlate = new BlockPressurePlate(70, BlockTextures.Stone, PressurePlateActiviationRule.MOBS, Material.Stone).setHardness(0.5F).setSoundGroup(SoundStoneFootstep).setBlockName("pressurePlate")
        .IgnoreMetaUpdates();

    public static readonly Block IronDoor = new BlockDoor(71, Material.Metal).setHardness(5.0F).setSoundGroup(SoundMetalFootstep).setBlockName("doorIron").disableStats().IgnoreMetaUpdates();

    public static readonly Block WoodenPressurePlate = new BlockPressurePlate(72, BlockTextures.OakPlanks, PressurePlateActiviationRule.EVERYTHING, Material.Wood).setHardness(0.5F).setSoundGroup(SoundWoodFootstep)
        .setBlockName("pressurePlate")
        .IgnoreMetaUpdates();

    public static readonly Block RedstoneOre = new BlockRedstoneOre(73, BlockTextures.RedstoneOre, false).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreRedstone").IgnoreMetaUpdates()
        .SetVariance(TextureVariance.Rotate180, TextureVariance.FlipBoth);

    public static readonly Block LitRedstoneOre = new BlockRedstoneOre(74, BlockTextures.RedstoneOre, true).setLuminance(10.0F / 16.0F).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreRedstone")
        .IgnoreMetaUpdates()
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block RedstoneTorch = new BlockRedstoneTorch(75, BlockTextures.RedstoneTorchUnlit, false).setHardness(0.0F).setSoundGroup(SoundWoodFootstep).setBlockName("notGate").IgnoreMetaUpdates();
    public static readonly Block LitRedstoneTorch = new BlockRedstoneTorch(76, BlockTextures.RedstoneTorchLit, true).setHardness(0.0F).setLuminance(0.5F).setSoundGroup(SoundWoodFootstep).setBlockName("notGate").IgnoreMetaUpdates();
    public static readonly Block Button = new BlockButton(77, BlockTextures.Stone).setHardness(0.5F).setSoundGroup(SoundStoneFootstep).setBlockName("button").IgnoreMetaUpdates();
    public static readonly Block Snow = new BlockSnow(78, BlockTextures.Snow).setHardness(0.1F).setSoundGroup(SoundClothFootstep).setBlockName("snow").SetVariance(TextureVariance.All, TextureVariance.FlipBoth);
    public static readonly Block Ice = new BlockIce(79, BlockTextures.Ice).setHardness(0.5F).setOpacity(3).setSoundGroup(SoundGlassFootstep).setBlockName("ice").SetVariance(TextureVariance.Rotate180);
    public static readonly Block SnowBlock = new BlockSnowBlock(80, BlockTextures.Snow).setHardness(0.2F).setSoundGroup(SoundClothFootstep).setBlockName("snow").SetVariance(TextureVariance.All, TextureVariance.FlipBoth);

    public static readonly Block Cactus = new BlockCactus(81, BlockTextures.CactusSide).setHardness(0.4F).setSoundGroup(SoundClothFootstep).setBlockName("cactus")
        .SetVariance(TextureVariance.All, TextureVariance.All, TextureVariance.Rotate180);

    public static readonly Block Clay = new BlockClay(82, BlockTextures.Clay).setHardness(0.6F).setSoundGroup(SoundGravelFootstep).setBlockName("clay").SetVariance(TextureVariance.Rotations);
    public static readonly Block SugarCane = new BlockReed(83, BlockTextures.SugarCane).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("reeds").disableStats();
    public static readonly Block Jukebox = new BlockJukeBox(84, BlockTextures.NoteBlock).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("jukebox").IgnoreMetaUpdates();
    public static readonly Block Fence = new BlockFence(85, BlockTextures.OakPlanks).setHardness(2.0F).setResistance(5.0F).setSoundGroup(SoundWoodFootstep).setBlockName("fence").IgnoreMetaUpdates();
    public static readonly Block Pumpkin = new BlockPumpkin(86, BlockTextures.PumpkinBase, false).setHardness(1.0F).setSoundGroup(SoundWoodFootstep).setBlockName("pumpkin").IgnoreMetaUpdates();
    public static readonly Block Netherrack = new BlockNetherrack(87, BlockTextures.Netherrack).setHardness(0.4F).setSoundGroup(SoundStoneFootstep).setBlockName("hellrock").SetVariance(TextureVariance.All);
    public static readonly Block Soulsand = new BlockSoulSand(88, BlockTextures.SoulSand).setHardness(0.5F).setSoundGroup(SoundSandFootstep).setBlockName("hellsand");

    public static readonly Block Glowstone = new BlockGlowstone(89, BlockTextures.Glowstone, Material.Stone).setHardness(0.3F).setSoundGroup(SoundGlassFootstep).setLuminance(1.0F).setBlockName("lightgem")
        .SetVariance(TextureVariance.All, TextureVariance.FlipBoth);

    public static readonly BlockPortal NetherPortal = (BlockPortal)new BlockPortal(90, BlockTextures.Portal).setHardness(-1.0F).setSoundGroup(SoundGlassFootstep).setLuminance(12.0F / 16.0F).setBlockName("portal");

    public static readonly Block JackLantern = new BlockPumpkin(91, BlockTextures.PumpkinBase, true).setHardness(1.0F).setSoundGroup(SoundWoodFootstep).setLuminance(1.0F).setBlockName("litpumpkin").IgnoreMetaUpdates()
        .SetVariance(TextureVariance.All, TextureVariance.None);

    public static readonly Block Cake = new BlockCake(92, BlockTextures.Cake).setHardness(0.5F).setSoundGroup(SoundClothFootstep).setBlockName("cake").disableStats().IgnoreMetaUpdates();
    public static readonly Block Repeater = new BlockRedstoneRepeater(93, false).setHardness(0.0F).setSoundGroup(SoundWoodFootstep).setBlockName("diode").disableStats().IgnoreMetaUpdates();
    public static readonly Block PoweredRepeater = new BlockRedstoneRepeater(94, true).setHardness(0.0F).setLuminance(10.0F / 16.0F).setSoundGroup(SoundWoodFootstep).setBlockName("diode").disableStats().IgnoreMetaUpdates();
    public static readonly Block Trapdoor = new BlockTrapDoor(96, Material.Wood).setHardness(3.0F).setSoundGroup(SoundWoodFootstep).setBlockName("trapdoor").disableStats().IgnoreMetaUpdates();

    public readonly int id;
    public readonly Material material;
    private string _blockName = "";
    public Box BoundingBox;
    public float hardness;
    public float particleFallSpeedModifier;
    public float resistance;
    protected bool shouldTrackStatistics;
    public float Slipperiness;
    public BlockSoundGroup SoundGroup;
    public int TextureId;

    static Block()
    {
        Item.ITEMS[Wool.id] = new ItemCloth(Wool.id - 256).setItemName("cloth");
        Item.ITEMS[Log.id] = new ItemLog(Log.id - 256).setItemName("log");
        Item.ITEMS[Slab.id] = new ItemSlab(Slab.id - 256).setItemName("stoneSlab");
        Item.ITEMS[Sapling.id] = new ItemSapling(Sapling.id - 256).setItemName("sapling");
        Item.ITEMS[Leaves.id] = new ItemLeaves(Leaves.id - 256).setItemName("leaves");
        Item.ITEMS[Piston.id] = new ItemPiston(Piston.id - 256);
        Item.ITEMS[StickyPiston.id] = new ItemPiston(StickyPiston.id - 256);

        for (int blockId = 0; blockId < 256; ++blockId)
        {
            if (Blocks[blockId] != null && Item.ITEMS[blockId] == null)
            {
                Item.ITEMS[blockId] = new ItemBlock(blockId - 256);
                Blocks[blockId].init();
            }
        }

        BlocksAllowVision[0] = true;
        Stats.Stats.InitializeItemStats();
    }

    protected Block(int id, Material material)
    {
        shouldTrackStatistics = true;
        SoundGroup = SoundPowderFootstep;
        particleFallSpeedModifier = 1.0F;
        Slipperiness = 0.6F;
        if (Blocks[id] != null)
        {
            throw new ArgumentException("Slot " + id + " is already occupied by " + Blocks[id] + " when adding " + this, nameof(id));
        }

        this.material = material;
        Blocks[id] = this;
        this.id = id;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        BlocksOpaque[id] = isOpaque();
        BlockLightOpacity[id] = isOpaque() ? 255 : 0;
        BlocksAllowVision[id] = !material.BlocksVision;
        BlocksWithEntity[id] = false;
    }

    protected Block(int id, int textureId, Material material) : this(id, material) => TextureId = textureId;

    public TextureVariance TopVariance { get; private set; } = TextureVariance.None;
    public TextureVariance BottomVariance { get; private set; } = TextureVariance.None;
    public TextureVariance SideVariance { get; private set; } = TextureVariance.None;

    protected Block IgnoreMetaUpdates()
    {
        BlocksIgnoreMetaUpdate[id] = true;
        return this;
    }

    protected virtual void init()
    {
    }

    protected Block setSoundGroup(BlockSoundGroup soundGroup)
    {
        this.SoundGroup = soundGroup;
        return this;
    }

    protected Block setOpacity(int opacity)
    {
        BlockLightOpacity[id] = opacity;
        return this;
    }

    protected Block setLuminance(float fractionalValue)
    {
        BlocksLightLuminance[id] = (int)(15.0F * fractionalValue);
        return this;
    }

    protected Block setResistance(float resistance)
    {
        this.resistance = resistance * 3.0F;
        return this;
    }

    public virtual bool isFullCube() => true;

    public virtual BlockRendererType getRenderType() => BlockRendererType.Standard;

    public Block SetVariance(TextureVariance allFaces)
    {
        TopVariance = allFaces;
        BottomVariance = allFaces;
        SideVariance = allFaces;
        return this;
    }

    public Block SetVariance(TextureVariance topBottom, TextureVariance sides)
    {
        TopVariance = topBottom;
        BottomVariance = topBottom;
        SideVariance = sides;
        return this;
    }

    public Block SetVariance(TextureVariance top, TextureVariance bottom, TextureVariance sides)
    {
        TopVariance = top;
        BottomVariance = bottom;
        SideVariance = sides;
        return this;
    }

    protected Block setHardness(float hardness)
    {
        this.hardness = hardness;
        if (resistance < hardness * 5.0F)
        {
            resistance = hardness * 5.0F;
        }

        return this;
    }

    protected Block setUnbreakable()
    {
        setHardness(-1.0F);
        return this;
    }

    public float getHardness() => hardness;

    protected Block setTickRandomly(bool tickRandomly)
    {
        BlocksRandomTick[id] = tickRandomly;
        return this;
    }

    public void setBoundingBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ) => BoundingBox = new Box(minX, minY, minZ, maxX, maxY, maxZ);

    public virtual float getLuminance(ILightProvider lighting, int x, int y, int z)
    {
        if (lighting != null)
        {
            return lighting.GetNaturalBrightness(x, y, z, BlocksLightLuminance[id]);
        }

        int baseLum = BlocksLightLuminance[id];
        return baseLum > 0 ? baseLum / 15.0f : 1.0f;
    }

    public virtual bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        double minX = BoundingBox.MinX;
        double minY = BoundingBox.MinY;
        double minZ = BoundingBox.MinZ;
        double maxX = BoundingBox.MaxX;
        double maxY = BoundingBox.MaxY;
        double maxZ = BoundingBox.MaxZ;

        if (!side.IsValidSide()) return !iBlockReader.IsOpaque(x, y, z);

        return side switch
        {
            Side.Down => minY > 0.0D || !iBlockReader.IsOpaque(x, y, z),
            Side.Up => maxY < 1.0D || !iBlockReader.IsOpaque(x, y, z),
            Side.North => minZ > 0.0D || !iBlockReader.IsOpaque(x, y, z),
            Side.South => maxZ < 1.0D || !iBlockReader.IsOpaque(x, y, z),
            Side.West => minX > 0.0D || !iBlockReader.IsOpaque(x, y, z),
            Side.East => maxX < 1.0D || !iBlockReader.IsOpaque(x, y, z),
            _ => !iBlockReader.IsOpaque(x, y, z)
        };
    }

    public virtual bool isSolidFace(IBlockReader iBlockReader, int x, int y, int z, int face) => iBlockReader.GetMaterial(x, y, z).IsSolid;

    public virtual int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side) => GetTexture(side, iBlockReader.GetBlockMeta(x, y, z));

    public virtual int GetTexture(Side side, int meta) => GetTexture(side);

    public virtual int GetTexture(Side side) => TextureId;

    public virtual Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z) => BoundingBox.Offset(x, y, z);

    public virtual void addIntersectingBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z, Box box, List<Box> boxes)
    {
        Box? collisionBox = getCollisionShape(world, entities, x, y, z);
        if (collisionBox != null && box.Intersects(collisionBox.Value))
        {
            boxes.Add(collisionBox.Value);
        }
    }

    public virtual Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => BoundingBox.Offset(x, y, z);

    public virtual bool isOpaque() => true;

    public virtual bool hasCollision(int meta, bool allowLiquids) => hasCollision();

    public virtual bool hasCollision() => true;

    public virtual void onTick(OnTickEvent e)
    {
    }

    public virtual void randomDisplayTick(OnTickEvent e)
    {
    }

    public virtual void onMetadataChange(OnMetadataChangeEvent ctx)
    {
    }

    public virtual void neighborUpdate(OnTickEvent e)
    {
    }

    public virtual int getTickRate() => 10;

    public virtual void onPlaced(OnPlacedEvent e)
    {
    }

    public virtual void onBreak(OnBreakEvent e)
    {
    }

    public virtual int getDroppedItemCount() => 1;

    public virtual int getDroppedItemId(int blockMeta) => id;

    public float getHardness(EntityPlayer player) => hardness < 0.0F ? 0.0F : !player.CanHarvest(this) ? 1.0F / hardness / 100.0F : player.GetBlockBreakingSpeed(this) / hardness / 30.0F;

    public virtual void dropStacks(OnDropEvent ctx)
    {
        if (!ctx.World.IsRemote && ctx.World.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            int dropCount = getDroppedItemCount();

            for (int attempt = 0; attempt < dropCount; ++attempt)
            {
                if (Random.Shared.NextSingle() <= ctx.Luck)
                {
                    int itemId = getDroppedItemId(ctx.Meta);
                    if (itemId > 0)
                    {
                        dropStack(ctx.World, ctx.X, ctx.Y, ctx.Z, new ItemStack(itemId, 1, getDroppedItemMeta(ctx.Meta)));
                    }
                }
            }
        }
    }

    protected static void dropStack(IWorldContext world, int x, int y, int z, ItemStack itemStack)
    {
        if (!world.IsRemote && world.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            float spreadFactor = 0.7F;
            double offsetX = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
            double offsetY = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
            double offsetZ = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
            world.SpawnItemDrop(x + offsetX, y + offsetY, z + offsetZ, itemStack);
        }
    }

    protected virtual int getDroppedItemMeta(int blockMeta) => 0;

    public virtual float getBlastResistance(Entity entity) => resistance / 5.0F;

    public virtual HitResult raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, entities, x, y, z);
        Vec3D pos = new(x, y, z);
        HitResult res = BoundingBox.Raycast(startPos - pos, endPos - pos);
        if (res.Type == HitResultType.MISS)
        {
            return new HitResult(HitResultType.MISS);
        }

        res.BlockX = x;
        res.BlockY = y;
        res.BlockZ = z;
        res.Pos += pos;
        return res;
    }

    public virtual void onDestroyedByExplosion(OnDestroyedByExplosionEvent @event)
    {
    }

    public virtual int getRenderLayer() => 0;

    public virtual bool canPlaceAt(CanPlaceAtContext evt)
    {
        int blockId = evt.World.Reader.GetBlockId(evt.X, evt.Y, evt.Z);
        return blockId == 0 || Blocks[blockId].material.IsReplaceable;
    }

    public virtual bool onUse(OnUseEvent _) => false;

    public virtual void onSteppedOn(OnEntityStepEvent @event)
    {
    }

    public virtual void onBlockBreakStart(OnBlockBreakStartEvent @event)
    {
    }

    public virtual Vec3D applyVelocity(OnApplyVelocityEvent @event) => Vec3D.Zero;

    public void updateBoundingBox(IBlockReader blockReader, int x, int y, int z) => updateBoundingBox(blockReader, null, x, y, z);

    public virtual void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
    }

    public virtual int getColor(int meta) => 0xFFFFFF;

    public virtual int getColorForFace(int meta, int face) => getColor(meta);

    public virtual int getColorMultiplier(IBlockReader iBlockReader, int x, int y, int z) => 0xFFFFFF;

    public virtual int getColorMultiplier(IBlockReader iBlockReader, int x, int y, int z, int knownMeta) => getColorMultiplier(iBlockReader, x, y, z);

    public virtual bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) => false;

    public virtual bool canEmitRedstonePower() => false;

    public virtual bool isFlammable(IBlockReader iBlockReader, int x, int y, int z) => false;

    public virtual void onEntityCollision(OnEntityCollisionEvent @event)
    {
    }

    public virtual bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => false;

    public virtual void setupRenderBoundingBox()
    {
    }

    public virtual void onAfterBreak(OnAfterBreakEvent ctx)
    {
        ctx.Player.IncreaseStat(Stats.Stats.MineBlockStatArray[id], 1);
        dropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, ctx.Meta));
    }

    public virtual bool canGrow(OnTickEvent ctx) => true;

    public Block setBlockName(string name)
    {
        _blockName = "tile." + name;
        return this;
    }

    public string translateBlockName() => StatCollector.TranslateToLocal($"{getBlockName()}.name");

    public string getBlockName() => _blockName;

    public virtual void onBlockAction(OnBlockActionEvent ctx)
    {
    }

    public bool getEnableStats() => shouldTrackStatistics;

    protected Block disableStats()
    {
        shouldTrackStatistics = false;
        return this;
    }

    public virtual int getPistonBehavior() => material.PistonBehavior;
}
