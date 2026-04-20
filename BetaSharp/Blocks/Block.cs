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

/// <summary>
///     Base class for all blocks, containing shared properties and methods.
///     Each block type is represented by a singleton instance of a subclass of this class, stored in the static fields of
///     this class.
/// </summary>
public class Block
{
    // Sound groups=
    public static readonly BlockSoundGroup SoundPowderFootstep = new("stone", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundWoodFootstep = new("wood", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundGravelFootstep = new("gravel", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundGrassFootstep = new("grass", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundStoneFootstep = new("stone", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundMetalFootstep = new("stone", 1.0F, 1.5F);
    public static readonly BlockSoundGroup SoundGlassFootstep = new("stone", 1.0F, 1.0F, "random.glass");
    public static readonly BlockSoundGroup SoundClothFootstep = new("cloth", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundSandFootstep = new("sand", 1.0F, 1.0F, "step.gravel");

    // Block arrays
    public static readonly Block[] Blocks = new Block[256];
    public static readonly bool[] BlocksRandomTick = new bool[256];
    public static readonly bool[] BlocksOpaque = new bool[256];
    public static readonly bool[] BlocksWithEntity = new bool[256];
    public static readonly int[] BlockLightOpacity = new int[256];
    public static readonly bool[] BlocksAllowVision = new bool[256];
    public static readonly int[] BlocksLightLuminance = new int[256];
    public static readonly bool[] BlocksIgnoreMetaUpdate = new bool[256];

    public static readonly Block Stone = new BlockStone(1, BlockTextures.Stone).SetHardness(1.5F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stone");
    public static readonly BlockGrass GrassBlock = (BlockGrass)new BlockGrass(2).SetHardness(0.6F).SetSoundGroup(SoundGrassFootstep).SetBlockName("grass").SetVariance(TextureVariance.Rotations, TextureVariance.All, TextureVariance.None);
    public static readonly Block Dirt = new BlockDirt(3).SetHardness(0.5F).SetSoundGroup(SoundGravelFootstep).SetBlockName("dirt").SetVariance(TextureVariance.All);
    public static readonly Block Cobblestone = new BlockCobblestone(4).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stonebrick");
    public static readonly Block Planks = new Block(5, BlockTextures.OakPlanks, Material.Wood).SetHardness(2.0F).SetResistance(5.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("wood").IgnoreMetaUpdates();
    public static readonly Block Sapling = new BlockSapling(6).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("sapling").IgnoreMetaUpdates();

    public static readonly Block Bedrock = new Block(7, BlockTextures.Bedrock, Material.Stone).SetUnbreakable().SetResistance(6000000.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("bedrock").DisableStats()
        .SetVariance(TextureVariance.All);

    public static readonly Block FlowingWater = new BlockFlowing(8, Material.Water).SetHardness(100.0F).SetOpacity(3).SetBlockName("water").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Water = new BlockStationary(9, Material.Water).SetHardness(100.0F).SetOpacity(3).SetBlockName("water").DisableStats().IgnoreMetaUpdates();
    public static readonly Block FlowingLava = new BlockFlowing(10, Material.Lava).SetHardness(0.0F).SetLuminance(1.0F).SetOpacity(255).SetBlockName("lava").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Lava = new BlockStationary(11, Material.Lava).SetHardness(100.0F).SetLuminance(1.0F).SetOpacity(255).SetBlockName("lava").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Sand = new BlockSand(12, BlockTextures.Sand).SetHardness(0.5F).SetSoundGroup(SoundSandFootstep).SetBlockName("sand").SetVariance(TextureVariance.Rotations);
    public static readonly Block Gravel = new BlockGravel(13, BlockTextures.Gravel).SetHardness(0.6F).SetSoundGroup(SoundGravelFootstep).SetBlockName("gravel").SetVariance(TextureVariance.Rotations);

    public static readonly Block GoldOre = new BlockOre(14, BlockTextures.GoldOre).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreGold")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block IronOre = new BlockOre(15, BlockTextures.IronOre).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreIron")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block CoalOre = new BlockOre(16, BlockTextures.CoalOre).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreCoal")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block Log = new BlockLog(17).SetHardness(2.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("log").IgnoreMetaUpdates().SetVariance(TextureVariance.All, TextureVariance.Rotate180);

    public static readonly BlockLeaves Leaves = (BlockLeaves)new BlockLeaves(18, BlockTextures.LeavesOak).SetHardness(0.2F).SetOpacity(1).SetSoundGroup(SoundGrassFootstep).SetBlockName("leaves").DisableStats().IgnoreMetaUpdates()
        .SetVariance(TextureVariance.All, TextureVariance.Rotate180);

    public static readonly Block Sponge = new BlockSponge(19).SetHardness(0.6F).SetSoundGroup(SoundGrassFootstep).SetBlockName("sponge").SetVariance(TextureVariance.All, TextureVariance.FlipBoth);
    public static readonly Block Glass = new BlockGlass(20, BlockTextures.Glass, Material.Glass, false).SetHardness(0.3F).SetSoundGroup(SoundGlassFootstep).SetBlockName("glass").SetVariance(TextureVariance.Rotate180);

    public static readonly Block LapisOre = new BlockOre(21, BlockTextures.LapisOre).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreLapis")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block LapisBlock = new Block(22, BlockTextures.BlockLapis, Material.Stone).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("blockLapis");
    public static readonly Block Dispenser = new BlockDispenser(23).SetHardness(3.5F).SetSoundGroup(SoundStoneFootstep).SetBlockName("dispenser").IgnoreMetaUpdates();
    public static readonly Block Sandstone = new BlockSandStone(24).SetSoundGroup(SoundStoneFootstep).SetHardness(0.8F).SetBlockName("sandStone").SetVariance(TextureVariance.Rotations, TextureVariance.None);
    public static readonly Block Noteblock = new BlockNote(25).SetHardness(0.8F).SetBlockName("musicBlock").IgnoreMetaUpdates();
    public static readonly Block Bed = new BlockBed(26).SetHardness(0.2F).SetBlockName("bed").DisableStats().IgnoreMetaUpdates();
    public static readonly Block PoweredRail = new BlockRail(27, BlockTextures.PoweredRailOn, true).SetHardness(0.7F).SetSoundGroup(SoundMetalFootstep).SetBlockName("goldenRail").IgnoreMetaUpdates();
    public static readonly Block DetectorRail = new BlockDetectorRail(28, BlockTextures.DetectorRail).SetHardness(0.7F).SetSoundGroup(SoundMetalFootstep).SetBlockName("detectorRail").IgnoreMetaUpdates();
    public static readonly Block StickyPiston = new BlockPistonBase(29, BlockTextures.PistonTopSticky, true).SetBlockName("pistonStickyBase").IgnoreMetaUpdates();
    public static readonly Block Cobweb = new BlockWeb(30, BlockTextures.Cobweb).SetOpacity(1).SetHardness(4.0F).SetBlockName("web");
    public static readonly BlockTallGrass Grass = (BlockTallGrass)new BlockTallGrass(31, BlockTextures.TallGrass).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("tallgrass");
    public static readonly BlockDeadBush DeadBush = (BlockDeadBush)new BlockDeadBush(32, BlockTextures.DeadBush).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("deadbush");
    public static readonly Block Piston = new BlockPistonBase(33, BlockTextures.PistonTopNormal, false).SetBlockName("pistonBase").IgnoreMetaUpdates();
    public static readonly BlockPistonExtension PistonHead = (BlockPistonExtension)new BlockPistonExtension(34, BlockTextures.PistonTopNormal).IgnoreMetaUpdates();
    public static readonly Block Wool = new BlockCloth().SetHardness(0.8F).SetSoundGroup(SoundClothFootstep).SetBlockName("cloth").IgnoreMetaUpdates().SetVariance(TextureVariance.None, TextureVariance.FlipBoth);
    public static readonly BlockPistonMoving MovingPiston = new(36);
    public static readonly BlockPlant Dandelion = (BlockPlant)new BlockPlant(37, BlockTextures.Dandelion).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("flower");
    public static readonly BlockPlant Rose = (BlockPlant)new BlockPlant(38, BlockTextures.Rose).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("rose");
    public static readonly BlockPlant BrownMushroom = (BlockPlant)new BlockMushroom(39, BlockTextures.BrownMushroom).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetLuminance(2.0F / 16.0F).SetBlockName("mushroom");
    public static readonly BlockPlant RedMushroom = (BlockPlant)new BlockMushroom(40, BlockTextures.RedMushroom).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("mushroom");
    public static readonly Block GoldBlock = new BlockOreStorage(41, BlockTextures.BlockGold).SetHardness(3.0F).SetResistance(10.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("blockGold");
    public static readonly Block IronBlock = new BlockOreStorage(42, BlockTextures.BlockIron).SetHardness(5.0F).SetResistance(10.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("blockIron");
    public static readonly Block DoubleSlab = new BlockSlab(43, true).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stoneSlab");
    public static readonly Block Slab = new BlockSlab(44, false).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stoneSlab");
    public static readonly Block Bricks = new Block(45, BlockTextures.Bricks, Material.Stone).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("brick");
    public static readonly Block TNT = new BlockTNT(46, BlockTextures.TntSide).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("tnt");
    public static readonly Block Bookshelf = new BlockBookshelf(47, BlockTextures.Bookshelf).SetHardness(1.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("bookshelf").SetVariance(TextureVariance.None, TextureVariance.FlipU);
    public static readonly Block MossyCobblestone = new Block(48, BlockTextures.MossyCobblestone, Material.Stone).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stoneMoss");
    public static readonly Block Obsidian = new BlockObsidian(49, BlockTextures.Obsidian).SetHardness(10.0F).SetResistance(2000.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("obsidian");
    public static readonly Block Torch = new BlockTorch(50, BlockTextures.Torch).SetHardness(0.0F).SetLuminance(15.0F / 16.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("torch").IgnoreMetaUpdates();
    public static readonly Block Fire = (BlockFire)new BlockFire(51, BlockTextures.Fire).SetHardness(0.0F).SetLuminance(1.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("fire").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Spawner = new BlockMobSpawner(52, BlockTextures.Spawner).SetHardness(5.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("mobSpawner").DisableStats();
    public static readonly Block WoodenStairs = new BlockStairs(53, Planks).SetBlockName("stairsWood").IgnoreMetaUpdates();
    public static readonly Block Chest = new BlockChest(54).SetHardness(2.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("chest").IgnoreMetaUpdates();
    public static readonly Block RedstoneWire = new BlockRedstoneWire(55, BlockTextures.RedstoneWireCross).SetHardness(0.0F).SetSoundGroup(SoundPowderFootstep).SetBlockName("redstoneDust").DisableStats().IgnoreMetaUpdates();

    public static readonly Block DiamondOre = new BlockOre(56, BlockTextures.DiamondOre).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreDiamond")
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block DiamondBlock = new BlockOreStorage(57, BlockTextures.BlockDiamond).SetHardness(5.0F).SetResistance(10.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("blockDiamond");
    public static readonly Block CraftingTable = new BlockWorkbench(58).SetHardness(2.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("workbench");
    public static readonly Block Wheat = new BlockCrops(59, BlockTextures.WheatStageBase).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("crops").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Farmland = new BlockFarmland(60).SetHardness(0.6F).SetSoundGroup(SoundGravelFootstep).SetBlockName("farmland");
    public static readonly Block Furnace = new BlockFurnace(61, false).SetHardness(3.5F).SetSoundGroup(SoundStoneFootstep).SetBlockName("furnace").IgnoreMetaUpdates();
    public static readonly Block LitFurnace = new BlockFurnace(62, true).SetHardness(3.5F).SetSoundGroup(SoundStoneFootstep).SetLuminance(14.0F / 16.0F).SetBlockName("furnace").IgnoreMetaUpdates();
    public static readonly Block Sign = new BlockSign(63, typeof(BlockEntitySign), true).SetHardness(1.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("sign").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Door = new BlockDoor(64, Material.Wood).SetHardness(3.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("doorWood").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Ladder = new BlockLadder(65, BlockTextures.Ladder).SetHardness(0.4F).SetSoundGroup(SoundWoodFootstep).SetBlockName("ladder").IgnoreMetaUpdates();
    public static readonly Block Rail = new BlockRail(66, BlockTextures.RailStraight, false).SetHardness(0.7F).SetSoundGroup(SoundMetalFootstep).SetBlockName("rail").IgnoreMetaUpdates();
    public static readonly Block CobblestoneStairs = new BlockStairs(67, Cobblestone).SetBlockName("stairsStone").IgnoreMetaUpdates();
    public static readonly Block WallSign = new BlockSign(68, typeof(BlockEntitySign), false).SetHardness(1.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("sign").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Lever = new BlockLever(69, BlockTextures.Lever).SetHardness(0.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("lever").IgnoreMetaUpdates();

    public static readonly Block StonePressurePlate = new BlockPressurePlate(70, BlockTextures.Stone, PressurePlateActiviationRule.MOBS, Material.Stone).SetHardness(0.5F).SetSoundGroup(SoundStoneFootstep).SetBlockName("pressurePlate")
        .IgnoreMetaUpdates();

    public static readonly Block IronDoor = new BlockDoor(71, Material.Metal).SetHardness(5.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("doorIron").DisableStats().IgnoreMetaUpdates();

    public static readonly Block WoodenPressurePlate = new BlockPressurePlate(72, BlockTextures.OakPlanks, PressurePlateActiviationRule.EVERYTHING, Material.Wood).SetHardness(0.5F).SetSoundGroup(SoundWoodFootstep)
        .SetBlockName("pressurePlate")
        .IgnoreMetaUpdates();

    public static readonly Block RedstoneOre = new BlockRedstoneOre(73, BlockTextures.RedstoneOre, false).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreRedstone").IgnoreMetaUpdates()
        .SetVariance(TextureVariance.Rotate180, TextureVariance.FlipBoth);

    public static readonly Block LitRedstoneOre = new BlockRedstoneOre(74, BlockTextures.RedstoneOre, true).SetLuminance(10.0F / 16.0F).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreRedstone")
        .IgnoreMetaUpdates()
        .SetVariance(TextureVariance.Rotate180, TextureVariance.Rotate180);

    public static readonly Block RedstoneTorch = new BlockRedstoneTorch(75, BlockTextures.RedstoneTorchUnlit, false).SetHardness(0.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("notGate").IgnoreMetaUpdates();
    public static readonly Block LitRedstoneTorch = new BlockRedstoneTorch(76, BlockTextures.RedstoneTorchLit, true).SetHardness(0.0F).SetLuminance(0.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("notGate").IgnoreMetaUpdates();
    public static readonly Block Button = new BlockButton(77, BlockTextures.Stone).SetHardness(0.5F).SetSoundGroup(SoundStoneFootstep).SetBlockName("button").IgnoreMetaUpdates();
    public static readonly Block Snow = new BlockSnow(78, BlockTextures.Snow).SetHardness(0.1F).SetSoundGroup(SoundClothFootstep).SetBlockName("snow").SetVariance(TextureVariance.All, TextureVariance.FlipBoth);
    public static readonly Block Ice = new BlockIce(79, BlockTextures.Ice).SetHardness(0.5F).SetOpacity(3).SetSoundGroup(SoundGlassFootstep).SetBlockName("ice").SetVariance(TextureVariance.Rotate180);
    public static readonly Block SnowBlock = new BlockSnowBlock(80, BlockTextures.Snow).SetHardness(0.2F).SetSoundGroup(SoundClothFootstep).SetBlockName("snow").SetVariance(TextureVariance.All, TextureVariance.FlipBoth);

    public static readonly Block Cactus = new BlockCactus(81, BlockTextures.CactusSide).SetHardness(0.4F).SetSoundGroup(SoundClothFootstep).SetBlockName("cactus")
        .SetVariance(TextureVariance.All, TextureVariance.All, TextureVariance.Rotate180);

    public static readonly Block Clay = new BlockClay(82, BlockTextures.Clay).SetHardness(0.6F).SetSoundGroup(SoundGravelFootstep).SetBlockName("clay").SetVariance(TextureVariance.Rotations);
    public static readonly Block SugarCane = new BlockReed(83, BlockTextures.SugarCane).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("reeds").DisableStats();
    public static readonly Block Jukebox = new BlockJukeBox(84, BlockTextures.NoteBlock).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("jukebox").IgnoreMetaUpdates();
    public static readonly Block Fence = new BlockFence(85, BlockTextures.OakPlanks).SetHardness(2.0F).SetResistance(5.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("fence").IgnoreMetaUpdates();
    public static readonly Block Pumpkin = new BlockPumpkin(86, BlockTextures.PumpkinBase, false).SetHardness(1.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("pumpkin").IgnoreMetaUpdates();
    public static readonly Block Netherrack = new BlockNetherrack(87, BlockTextures.Netherrack).SetHardness(0.4F).SetSoundGroup(SoundStoneFootstep).SetBlockName("hellrock").SetVariance(TextureVariance.All);
    public static readonly Block Soulsand = new BlockSoulSand(88, BlockTextures.SoulSand).SetHardness(0.5F).SetSoundGroup(SoundSandFootstep).SetBlockName("hellsand");

    public static readonly Block Glowstone = new BlockGlowstone(89, BlockTextures.Glowstone, Material.Stone).SetHardness(0.3F).SetSoundGroup(SoundGlassFootstep).SetLuminance(1.0F).SetBlockName("lightgem")
        .SetVariance(TextureVariance.All, TextureVariance.FlipBoth);

    public static readonly BlockPortal NetherPortal = (BlockPortal)new BlockPortal(90, BlockTextures.Portal).SetHardness(-1.0F).SetSoundGroup(SoundGlassFootstep).SetLuminance(12.0F / 16.0F).SetBlockName("portal");

    public static readonly Block JackLantern = new BlockPumpkin(91, BlockTextures.PumpkinBase, true).SetHardness(1.0F).SetSoundGroup(SoundWoodFootstep).SetLuminance(1.0F).SetBlockName("litpumpkin").IgnoreMetaUpdates()
        .SetVariance(TextureVariance.All, TextureVariance.None);

    public static readonly Block Cake = new BlockCake(92, BlockTextures.Cake).SetHardness(0.5F).SetSoundGroup(SoundClothFootstep).SetBlockName("cake").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Repeater = new BlockRedstoneRepeater(93, false).SetHardness(0.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("diode").DisableStats().IgnoreMetaUpdates();
    public static readonly Block PoweredRepeater = new BlockRedstoneRepeater(94, true).SetHardness(0.0F).SetLuminance(10.0F / 16.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("diode").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Trapdoor = new BlockTrapDoor(96, Material.Wood).SetHardness(3.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("trapdoor").DisableStats().IgnoreMetaUpdates();

    protected bool ShouldTrackStatistics;

    static Block()
    {
        Item.ITEMS[Wool.ID] = new ItemCloth(Wool.ID - 256).setItemName("cloth");
        Item.ITEMS[Log.ID] = new ItemLog(Log.ID - 256).setItemName("log");
        Item.ITEMS[Slab.ID] = new ItemSlab(Slab.ID - 256).setItemName("stoneSlab");
        Item.ITEMS[Sapling.ID] = new ItemSapling(Sapling.ID - 256).setItemName("sapling");
        Item.ITEMS[Leaves.ID] = new ItemLeaves(Leaves.ID - 256).setItemName("leaves");
        Item.ITEMS[Piston.ID] = new ItemPiston(Piston.ID - 256);
        Item.ITEMS[StickyPiston.ID] = new ItemPiston(StickyPiston.ID - 256);

        for (int blockId = 0; blockId < 256; ++blockId)
        {
            if (Blocks[blockId] != null && Item.ITEMS[blockId] == null)
            {
                Item.ITEMS[blockId] = new ItemBlock(blockId - 256);
                Blocks[blockId].Init();
            }
        }

        BlocksAllowVision[0] = true;
        Stats.Stats.InitializeItemStats();
    }

    protected Block(int id, Material material)
    {
        ShouldTrackStatistics = true;
        SoundGroup = SoundPowderFootstep;
        ParticleFallSpeedModifier = 1.0F;
        Slipperiness = 0.6F;
        if (Blocks[id] != null)
        {
            throw new ArgumentException("Slot " + id + " is already occupied by " + Blocks[id] + " when adding " + this, nameof(id));
        }

        Material = material;
        Blocks[id] = this;
        ID = id;
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        BlocksOpaque[id] = IsOpaque();
        BlockLightOpacity[id] = IsOpaque() ? 255 : 0;
        BlocksAllowVision[id] = !material.BlocksVision;
        BlocksWithEntity[id] = false;
    }

    protected Block(int id, int textureId, Material material) : this(id, material) => TextureId = textureId;

    public int ID { get; set; }
    public string Name { get; set; } = "";

    /// <summary>
    ///     Determines how this block behaves, check <see cref="Material" /> for more info.
    /// </summary>
    public Material Material { get; }


    /// <summary>
    ///     Used for collision and ray tracing.
    ///     By default, this is a full block (0, 0, 0) to (1, 1, 1), but can be changed for blocks with smaller or larger
    ///     hitboxes.
    /// </summary>
    public Box BoundingBox { get; set; }

    /// <summary>
    ///     Determines how long it takes to break the block.
    ///     A hardness of -1 means the block is unbreakable. (as set in <see cref="SetUnbreakable" />)
    /// </summary>
    public float Hardness { get; set; }

    /// <summary>
    ///     Const modifier applied to the speed of particles falling on this block.=
    /// </summary>
    public float ParticleFallSpeedModifier { get; set; }

    /// <summary>
    ///     Determines how resistant the block is to explosions.
    /// </summary>
    public float Resistance { get; set; }

    /// <summary>
    ///     How slippery this block is, which determines how much entities will slide when walking on this block.
    ///     Default is 0.6f, which is the slipperiness of most blocks. A value of 0.98f is the slipperiness of ice, while a
    ///     value of 0.0f means no slipperiness at all.
    /// </summary>
    public float Slipperiness { get; set; }

    /// <summary>
    ///     A <see cref="BlockSoundGroup" /> defining the sounds this block makes when walked on, broken, or placed.
    /// </summary>
    public BlockSoundGroup SoundGroup { get; set; }

    /// <summary>
    ///     Texture ID of this block, which determines which texture is used to render this block in the world and in the
    ///     inventory.
    /// </summary>
    public int TextureId { get; set; }

    public TextureVariance TopVariance { get; private set; } = TextureVariance.None;

    public TextureVariance BottomVariance { get; private set; } = TextureVariance.None;

    public TextureVariance SideVariance { get; private set; } = TextureVariance.None;

    /// <summary>
    ///     Make this block ignore meta updates.
    /// </summary>
    protected Block IgnoreMetaUpdates()
    {
        BlocksIgnoreMetaUpdate[ID] = true;
        return this;
    }

    protected virtual void Init()
    {
    }

    /// <summary>
    ///     Set the sound group for this block, which determines the sounds this block makes when walked on, broken, or placed.
    /// </summary>
    protected Block SetSoundGroup(BlockSoundGroup soundGroup)
    {
        SoundGroup = soundGroup;
        return this;
    }

    /// <summary>
    ///     Set the light opacity of this block, which determines how much light is blocked by this block.
    /// </summary>
    protected Block SetOpacity(int opacity)
    {
        BlockLightOpacity[ID] = opacity;
        return this;
    }

    /// <summary>
    ///     Set the light luminance of this block, which determines how much light is emitted by this block.
    /// </summary>
    protected Block SetLuminance(float fractionalValue)
    {
        BlocksLightLuminance[ID] = (int)(15.0F * fractionalValue);
        return this;
    }

    /// <summary>
    ///     Set the resistance of this block, which determines how resistant the block is to explosions.
    ///     Since resistance is set to hardness * 5.0F on every hardness set, this multiples
    ///     the input value by 3.0F for blocks not coupled to the hardness for resistance.
    /// </summary>
    /// <param name="resistance"></param>
    /// <returns></returns>
    protected Block SetResistance(float resistance)
    {
        Resistance = resistance * 3.0F;
        return this;
    }

    public virtual bool IsFullCube() => true;

    public virtual BlockRendererType GetRenderType() => BlockRendererType.Standard;

    /// <summary>
    ///     Set texture variance for all faces of this block to the same value.
    /// </summary>
    public Block SetVariance(TextureVariance allFaces)
    {
        TopVariance = allFaces;
        BottomVariance = allFaces;
        SideVariance = allFaces;
        return this;
    }

    /// <summary>
    ///     Set texture variance for the top and bottom faces of this block to the same value, and the sides to a different
    ///     value.
    /// </summary>
    public Block SetVariance(TextureVariance topBottom, TextureVariance sides)
    {
        TopVariance = topBottom;
        BottomVariance = topBottom;
        SideVariance = sides;
        return this;
    }

    /// <summary>
    ///     Set texture variance for the top, bottom, and sides of this block to different values.
    /// </summary>
    public Block SetVariance(TextureVariance top, TextureVariance bottom, TextureVariance sides)
    {
        TopVariance = top;
        BottomVariance = bottom;
        SideVariance = sides;
        return this;
    }

    /// <summary>
    ///     Set the hardness of this block, which determines how long it takes to break the block.
    ///     A hardness of -1 means the block is unbreakable. (as set in <see cref="SetUnbreakable" />)
    /// </summary>
    /// <remarks>
    ///     If the Resistance of this block is less than hardness * 5.0F, then the Resistance will be set to hardness * 5.0F.
    ///     <param name="hardness"></param>
    ///     <returns></returns>
    protected Block SetHardness(float hardness)
    {
        Hardness = hardness;
        if (Resistance < hardness * 5.0F)
        {
            Resistance = hardness * 5.0F;
        }

        return this;
    }

    /// <summary>
    ///     Make this block unbreakable (e.g. bedrock) by setting the hardness to -1.0F.
    /// </summary>
    protected Block SetUnbreakable()
    {
        SetHardness(-1.0F);
        return this;
    }

    /// <summary>
    ///     Make this block only have the Tick method called randomly, instead of every tick.
    ///     This is used for blocks like crops and fire that only need to update occasionally.
    /// </summary>
    protected Block SetTickRandomly(bool tickRandomly)
    {
        BlocksRandomTick[ID] = tickRandomly;
        return this;
    }

    /// <summary>
    ///     Set the bounding box of this block.
    ///     By default, this is a full block from (0, 0, 0) to (1, 1, 1), but can be changed for blocks with smaller or larger
    ///     hitboxes.
    /// </summary>
    public void SetBoundingBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ) =>
        BoundingBox = new Box(minX, minY, minZ, maxX, maxY, maxZ);

    /// <summary>
    ///     Get total luminace of this block.
    /// </summary>
    public virtual float GetLuminance(ILightProvider lighting, int x, int y, int z)
    {
        if (lighting != null)
        {
            return lighting.GetNaturalBrightness(x, y, z, BlocksLightLuminance[ID]);
        }

        int baseLum = BlocksLightLuminance[ID];
        return baseLum > 0 ? baseLum / 15.0f : 1.0f;
    }

    public virtual bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        double minX = BoundingBox.MinX;
        double minY = BoundingBox.MinY;
        double minZ = BoundingBox.MinZ;
        double maxX = BoundingBox.MaxX;
        double maxY = BoundingBox.MaxY;
        double maxZ = BoundingBox.MaxZ;

        if (!side.IsValidSide())
        {
            return !iBlockReader.IsOpaque(x, y, z);
        }

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

    public virtual bool IsSolidFace(IBlockReader iBlockReader, int x, int y, int z, int face) => iBlockReader.GetMaterial(x, y, z).IsSolid;

    public virtual int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side) => GetTexture(side, iBlockReader.GetBlockMeta(x, y, z));

    public virtual int GetTexture(Side side, int meta) => GetTexture(side);

    public virtual int GetTexture(Side side) => TextureId;

    public virtual Box GetBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z) => BoundingBox.Offset(x, y, z);

    public virtual void AddIntersectingBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z, Box box, List<Box> boxes)
    {
        Box? collisionBox = GetCollisionShape(world, entities, x, y, z);
        if (collisionBox != null && box.Intersects(collisionBox.Value))
        {
            boxes.Add(collisionBox.Value);
        }
    }

    public virtual Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => BoundingBox.Offset(x, y, z);

    public virtual bool IsOpaque() => true;

    public virtual bool HasCollision(int meta, bool allowLiquids) => HasCollision();

    public virtual bool HasCollision() => true;

    public virtual void OnTick(OnTickEvent e)
    {
    }

    public virtual void RandomDisplayTick(OnTickEvent e)
    {
    }

    public virtual void OnMetadataChange(OnMetadataChangeEvent ctx)
    {
    }

    public virtual void NeighborUpdate(OnTickEvent e)
    {
    }

    public virtual int GetTickRate() => 10;

    public virtual void OnPlaced(OnPlacedEvent e)
    {
    }

    public virtual void OnBreak(OnBreakEvent e)
    {
    }

    public virtual int GetDroppedItemCount() => 1;

    public virtual int GetDroppedItemId(int blockMeta) => ID;

    public float GetHardness(EntityPlayer player) => Hardness < 0.0F ? 0.0F : !player.canHarvest(this) ? 1.0F / Hardness / 100.0F : player.getBlockBreakingSpeed(this) / Hardness / 30.0F;

    public virtual void DropStacks(OnDropEvent ctx)
    {
        if (!ctx.World.IsRemote && ctx.World.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            int dropCount = GetDroppedItemCount();

            for (int attempt = 0; attempt < dropCount; ++attempt)
            {
                if (Random.Shared.NextSingle() <= ctx.Luck)
                {
                    int itemId = GetDroppedItemId(ctx.Meta);
                    if (itemId > 0)
                    {
                        DropStack(ctx.World, ctx.X, ctx.Y, ctx.Z, new ItemStack(itemId, 1, GetDroppedItemMeta(ctx.Meta)));
                    }
                }
            }
        }
    }

    protected static void DropStack(IWorldContext world, int x, int y, int z, ItemStack itemStack)
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

    protected virtual int GetDroppedItemMeta(int blockMeta) => 0;

    public virtual float GetBlastResistance(Entity entity) => Resistance / 5.0F;

    public virtual HitResult Raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        UpdateBoundingBox(world, entities, x, y, z);
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

    public virtual void OnDestroyedByExplosion(OnDestroyedByExplosionEvent @event)
    {
    }

    public virtual int GetRenderLayer() => 0;

    public virtual bool CanPlaceAt(CanPlaceAtContext evt)
    {
        int blockId = evt.World.Reader.GetBlockId(evt.X, evt.Y, evt.Z);
        return blockId == 0 || Blocks[blockId].Material.IsReplaceable;
    }

    public virtual bool OnUse(OnUseEvent _) => false;

    public virtual void OnSteppedOn(OnEntityStepEvent @event)
    {
    }

    public virtual void OnBlockBreakStart(OnBlockBreakStartEvent @event)
    {
    }

    public virtual Vec3D ApplyVelocity(OnApplyVelocityEvent @event) => Vec3D.Zero;

    public void UpdateBoundingBox(IBlockReader blockReader, int x, int y, int z) => UpdateBoundingBox(blockReader, null, x, y, z);

    public virtual void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
    }

    public virtual int GetColor(int meta) => 0xFFFFFF;

    public virtual int GetColorForFace(int meta, int face) => GetColor(meta);

    public virtual int GetColorMultiplier(IBlockReader iBlockReader, int x, int y, int z) => 0xFFFFFF;

    public virtual int GetColorMultiplier(IBlockReader iBlockReader, int x, int y, int z, int knownMeta) => GetColorMultiplier(iBlockReader, x, y, z);

    public virtual bool IsPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) => false;

    public virtual bool CanEmitRedstonePower() => false;

    public virtual bool IsFlammable(IBlockReader iBlockReader, int x, int y, int z) => false;

    public virtual void OnEntityCollision(OnEntityCollisionEvent @event)
    {
    }

    public virtual bool IsStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => false;

    public virtual void SetupRenderBoundingBox()
    {
    }

    public virtual void OnAfterBreak(OnAfterBreakEvent ctx)
    {
        ctx.Player.increaseStat(Stats.Stats.MineBlockStatArray[ID], 1);
        DropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, ctx.Meta));
    }

    public virtual bool CanGrow(OnTickEvent ctx) => true;

    public Block SetBlockName(string name)
    {
        Name = "tile." + name;
        return this;
    }

    public string TranslateBlockName() => StatCollector.TranslateToLocal($"{GetBlockName()}.name");

    public string GetBlockName() => Name;

    public virtual void OnBlockAction(OnBlockActionEvent ctx)
    {
    }

    public bool GetEnableStats() => ShouldTrackStatistics;

    protected Block DisableStats()
    {
        ShouldTrackStatistics = false;
        return this;
    }

    public virtual PistonBehavior GetPistonBehavior() => Material.PistonBehavior;
}
