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

    public static readonly Block?[] Blocks = new Block[256];
    public static readonly bool[] BlocksRandomTick = new bool[256];
    public static readonly bool[] BlocksOpaque = new bool[256];
    public static readonly bool[] BlocksWithEntity = new bool[256];
    public static readonly int[] BlockLightOpacity = new int[256];
    public static readonly bool[] BlocksAllowVision = new bool[256];
    public static readonly int[] BlocksLightLuminance = new int[256];
    public static readonly bool[] BlocksIgnoreMetaUpdate = new bool[256];

    public static readonly Block Stone = new BlockStone(1, 1).SetHardness(1.5F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stone");
    public static readonly BlockGrass GrassBlock = (BlockGrass)new BlockGrass(2).SetHardness(0.6F).SetSoundGroup(SoundGrassFootstep).SetBlockName("grass");
    public static readonly Block Dirt = new BlockDirt(3, 2).SetHardness(0.5F).SetSoundGroup(SoundGravelFootstep).SetBlockName("dirt");
    public static readonly Block Cobblestone = new Block(4, 16, Material.Stone).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stonebrick");
    public static readonly Block Planks = new Block(5, 4, Material.Wood).SetHardness(2.0F).SetResistance(5.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("wood").IgnoreMetaUpdates();
    public static readonly Block Sapling = new BlockSapling(6, 15).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("sapling").IgnoreMetaUpdates();
    public static readonly Block Bedrock = new Block(7, 17, Material.Stone).SetUnbreakable().SetResistance(6000000.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("bedrock").DisableStats();
    public static readonly Block FlowingWater = new BlockFlowing(8, Material.Water).SetHardness(100.0F).SetOpacity(3).SetBlockName("water").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Water = new BlockStationary(9, Material.Water).SetHardness(100.0F).SetOpacity(3).SetBlockName("water").DisableStats().IgnoreMetaUpdates();
    public static readonly Block FlowingLava = new BlockFlowing(10, Material.Lava).SetHardness(0.0F).SetLuminance(1.0F).SetOpacity(255).SetBlockName("lava").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Lava = new BlockStationary(11, Material.Lava).SetHardness(100.0F).SetLuminance(1.0F).SetOpacity(255).SetBlockName("lava").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Sand = new BlockSand(12, 18).SetHardness(0.5F).SetSoundGroup(SoundSandFootstep).SetBlockName("sand");
    public static readonly Block Gravel = new BlockGravel(13, 19).SetHardness(0.6F).SetSoundGroup(SoundGravelFootstep).SetBlockName("gravel");
    public static readonly Block GoldOre = new BlockOre(14, 32).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreGold");
    public static readonly Block IronOre = new BlockOre(15, 33).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreIron");
    public static readonly Block CoalOre = new BlockOre(16, 34).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreCoal");
    public static readonly Block Log = new BlockLog(17).SetHardness(2.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("log").IgnoreMetaUpdates();
    public static readonly BlockLeaves Leaves = (BlockLeaves)new BlockLeaves(18, 52).SetHardness(0.2F).SetOpacity(1).SetSoundGroup(SoundGrassFootstep).SetBlockName("leaves").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Sponge = new BlockSponge(19).SetHardness(0.6F).SetSoundGroup(SoundGrassFootstep).SetBlockName("sponge");
    public static readonly Block Glass = new BlockGlass(20, 49, Material.Glass, false).SetHardness(0.3F).SetSoundGroup(SoundGlassFootstep).SetBlockName("glass");
    public static readonly Block LapisOre = new BlockOre(21, 160).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreLapis");
    public static readonly Block LapisBlock = new Block(22, 144, Material.Stone).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("blockLapis");
    public static readonly Block Dispenser = new BlockDispenser(23).SetHardness(3.5F).SetSoundGroup(SoundStoneFootstep).SetBlockName("dispenser").IgnoreMetaUpdates();
    public static readonly Block Sandstone = new BlockSandStone(24).SetSoundGroup(SoundStoneFootstep).SetHardness(0.8F).SetBlockName("sandStone");
    public static readonly Block NoteBlock = new BlockNote(25).SetHardness(0.8F).SetBlockName("musicBlock").IgnoreMetaUpdates();
    public static readonly Block Bed = new BlockBed(26).SetHardness(0.2F).SetBlockName("bed").DisableStats().IgnoreMetaUpdates();
    public static readonly Block PoweredRail = new BlockRail(27, 179, true).SetHardness(0.7F).SetSoundGroup(SoundMetalFootstep).SetBlockName("goldenRail").IgnoreMetaUpdates();
    public static readonly Block DetectorRail = new BlockDetectorRail(28, 195).SetHardness(0.7F).SetSoundGroup(SoundMetalFootstep).SetBlockName("detectorRail").IgnoreMetaUpdates();
    public static readonly Block StickyPiston = new BlockPistonBase(29, 106, true).SetBlockName("pistonStickyBase").IgnoreMetaUpdates();
    public static readonly Block Cobweb = new BlockWeb(30, 11).SetOpacity(1).SetHardness(4.0F).SetBlockName("web");
    public static readonly BlockTallGrass Grass = (BlockTallGrass)new BlockTallGrass(31, 39).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("tallgrass");
    public static readonly BlockDeadBush DeadBush = (BlockDeadBush)new BlockDeadBush(32, 55).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("deadbush");
    public static readonly Block Piston = new BlockPistonBase(33, 107, false).SetBlockName("pistonBase").IgnoreMetaUpdates();
    public static readonly BlockPistonExtension PistonHead = (BlockPistonExtension)new BlockPistonExtension(34, 107).IgnoreMetaUpdates();
    public static readonly Block Wool = new BlockCloth().SetHardness(0.8F).SetSoundGroup(SoundClothFootstep).SetBlockName("cloth").IgnoreMetaUpdates();
    public static readonly BlockPistonMoving MovingPiston = new(36);
    public static readonly BlockPlant Dandelion = (BlockPlant)new BlockPlant(37, 13).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("flower");
    public static readonly BlockPlant Rose = (BlockPlant)new BlockPlant(38, 12).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("rose");
    public static readonly BlockPlant BrownMushroom = (BlockPlant)new BlockMushroom(39, 29).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetLuminance(2.0F / 16.0F).SetBlockName("mushroom");
    public static readonly BlockPlant RedMushroom = (BlockPlant)new BlockMushroom(40, 28).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("mushroom");
    public static readonly Block GoldBlock = new BlockOreStorage(41, 23).SetHardness(3.0F).SetResistance(10.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("blockGold");
    public static readonly Block IronBlock = new BlockOreStorage(42, 22).SetHardness(5.0F).SetResistance(10.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("blockIron");
    public static readonly Block DoubleSlab = new BlockSlab(43, true).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stoneSlab");
    public static readonly Block Slab = new BlockSlab(44, false).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stoneSlab");
    public static readonly Block Bricks = new Block(45, 7, Material.Stone).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("brick");
    public static readonly Block TNT = new BlockTNT(46, 8).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("tnt");
    public static readonly Block Bookshelf = new BlockBookshelf(47, 35).SetHardness(1.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("bookshelf");
    public static readonly Block MossyCobblestone = new Block(48, 36, Material.Stone).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("stoneMoss");
    public static readonly Block Obsidian = new BlockObsidian(49, 37).SetHardness(10.0F).SetResistance(2000.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("obsidian");
    public static readonly Block Torch = new BlockTorch(50, 80).SetHardness(0.0F).SetLuminance(15.0F / 16.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("torch").IgnoreMetaUpdates();
    public static readonly Block Fire = (BlockFire)new BlockFire(51, 31).SetHardness(0.0F).SetLuminance(1.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("fire").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Spawner = new BlockMobSpawner(52, 65).SetHardness(5.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("mobSpawner").DisableStats();
    public static readonly Block WoodenStairs = new BlockStairs(53, Planks).SetBlockName("stairsWood").IgnoreMetaUpdates();
    public static readonly Block Chest = new BlockChest(54).SetHardness(2.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("chest").IgnoreMetaUpdates();
    public static readonly Block RedstoneWire = new BlockRedstoneWire(55, 164).SetHardness(0.0F).SetSoundGroup(SoundPowderFootstep).SetBlockName("redstoneDust").DisableStats().IgnoreMetaUpdates();
    public static readonly Block DiamondOre = new BlockOre(56, 50).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreDiamond");
    public static readonly Block DiamondBlock = new BlockOreStorage(57, 24).SetHardness(5.0F).SetResistance(10.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("blockDiamond");
    public static readonly Block CraftingTable = new BlockWorkbench(58).SetHardness(2.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("workbench");
    public static readonly Block Wheat = new BlockCrops(59, 88).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("crops").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Farmland = new BlockFarmland(60).SetHardness(0.6F).SetSoundGroup(SoundGravelFootstep).SetBlockName("farmland");
    public static readonly Block Furnace = new BlockFurnace(61, false).SetHardness(3.5F).SetSoundGroup(SoundStoneFootstep).SetBlockName("furnace").IgnoreMetaUpdates();
    public static readonly Block LitFurnace = new BlockFurnace(62, true).SetHardness(3.5F).SetSoundGroup(SoundStoneFootstep).SetLuminance(14.0F / 16.0F).SetBlockName("furnace").IgnoreMetaUpdates();
    public static readonly Block Sign = new BlockSign(63, typeof(BlockEntitySign), true).SetHardness(1.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("sign").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Door = new BlockDoor(64, Material.Wood).SetHardness(3.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("doorWood").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Ladder = new BlockLadder(65, 83).SetHardness(0.4F).SetSoundGroup(SoundWoodFootstep).SetBlockName("ladder").IgnoreMetaUpdates();
    public static readonly Block Rail = new BlockRail(66, 128, false).SetHardness(0.7F).SetSoundGroup(SoundMetalFootstep).SetBlockName("rail").IgnoreMetaUpdates();
    public static readonly Block CobblestoneStairs = new BlockStairs(67, Cobblestone).SetBlockName("stairsStone").IgnoreMetaUpdates();
    public static readonly Block WallSign = new BlockSign(68, typeof(BlockEntitySign), false).SetHardness(1.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("sign").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Lever = new BlockLever(69, 96).SetHardness(0.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("lever").IgnoreMetaUpdates();
    public static readonly Block StonePressurePlate = new BlockPressurePlate(70, Stone.TextureId, PressurePlateActiviationRule.MOBS, Material.Stone).SetHardness(0.5F).SetSoundGroup(SoundStoneFootstep).SetBlockName("pressurePlate") .IgnoreMetaUpdates();
    public static readonly Block IronDoor = new BlockDoor(71, Material.Metal).SetHardness(5.0F).SetSoundGroup(SoundMetalFootstep).SetBlockName("doorIron").DisableStats().IgnoreMetaUpdates();
    public static readonly Block WoodenPressurePlate = new BlockPressurePlate(72, Planks.TextureId, PressurePlateActiviationRule.EVERYTHING, Material.Wood).SetHardness(0.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("pressurePlate") .IgnoreMetaUpdates();
    public static readonly Block RedstoneOre = new BlockRedstoneOre(73, 51, false).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreRedstone").IgnoreMetaUpdates();
    public static readonly Block LitRedstoneOre = new BlockRedstoneOre(74, 51, true).SetLuminance(10.0F / 16.0F).SetHardness(3.0F).SetResistance(5.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("oreRedstone").IgnoreMetaUpdates();
    public static readonly Block RedstoneTorch = new BlockRedstoneTorch(75, 115, false).SetHardness(0.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("notGate").IgnoreMetaUpdates();
    public static readonly Block LitRedstoneTorch = new BlockRedstoneTorch(76, 99, true).SetHardness(0.0F).SetLuminance(0.5F).SetSoundGroup(SoundWoodFootstep).SetBlockName("notGate").IgnoreMetaUpdates();
    public static readonly Block Button = new BlockButton(77, Stone.TextureId).SetHardness(0.5F).SetSoundGroup(SoundStoneFootstep).SetBlockName("button").IgnoreMetaUpdates();
    public static readonly Block Snow = new BlockSnow(78, 66).SetHardness(0.1F).SetSoundGroup(SoundClothFootstep).SetBlockName("snow");
    public static readonly Block Ice = new BlockIce(79, 67).SetHardness(0.5F).SetOpacity(3).SetSoundGroup(SoundGlassFootstep).SetBlockName("ice");
    public static readonly Block SnowBlock = new BlockSnowBlock(80, 66).SetHardness(0.2F).SetSoundGroup(SoundClothFootstep).SetBlockName("snow");
    public static readonly Block Cactus = new BlockCactus(81, 70).SetHardness(0.4F).SetSoundGroup(SoundClothFootstep).SetBlockName("cactus");
    public static readonly Block Clay = new BlockClay(82, 72).SetHardness(0.6F).SetSoundGroup(SoundGravelFootstep).SetBlockName("clay");
    public static readonly Block SugarCane = new BlockReed(83, 73).SetHardness(0.0F).SetSoundGroup(SoundGrassFootstep).SetBlockName("reeds").DisableStats();
    public static readonly Block Jukebox = new BlockJukeBox(84, 74).SetHardness(2.0F).SetResistance(10.0F).SetSoundGroup(SoundStoneFootstep).SetBlockName("jukebox").IgnoreMetaUpdates();
    public static readonly Block Fence = new BlockFence(85, 4).SetHardness(2.0F).SetResistance(5.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("fence").IgnoreMetaUpdates();
    public static readonly Block Pumpkin = new BlockPumpkin(86, 102, false).SetHardness(1.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("pumpkin").IgnoreMetaUpdates();
    public static readonly Block Netherrack = new BlockNetherrack(87, 103).SetHardness(0.4F).SetSoundGroup(SoundStoneFootstep).SetBlockName("hellrock");
    public static readonly Block SoulSand = new BlockSoulSand(88, 104).SetHardness(0.5F).SetSoundGroup(SoundSandFootstep).SetBlockName("hellsand");
    public static readonly Block Glowstone = new BlockGlowstone(89, 105, Material.Stone).SetHardness(0.3F).SetSoundGroup(SoundGlassFootstep).SetLuminance(1.0F).SetBlockName("lightgem");
    public static readonly BlockPortal NetherPortal = (BlockPortal)new BlockPortal(90, 14).SetHardness(-1.0F).SetSoundGroup(SoundGlassFootstep).SetLuminance(12.0F / 16.0F).SetBlockName("portal");
    public static readonly Block JackLantern = new BlockPumpkin(91, 102, true).SetHardness(1.0F).SetSoundGroup(SoundWoodFootstep).SetLuminance(1.0F).SetBlockName("litpumpkin").IgnoreMetaUpdates();
    public static readonly Block Cake = new BlockCake(92, 121).SetHardness(0.5F).SetSoundGroup(SoundClothFootstep).SetBlockName("cake").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Repeater = new BlockRedstoneRepeater(93, false).SetHardness(0.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("diode").DisableStats().IgnoreMetaUpdates();
    public static readonly Block PoweredRepeater = new BlockRedstoneRepeater(94, true).SetHardness(0.0F).SetLuminance(10.0F / 16.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("diode").DisableStats().IgnoreMetaUpdates();
    public static readonly Block Trapdoor = new BlockTrapDoor(96, Material.Wood).SetHardness(3.0F).SetSoundGroup(SoundWoodFootstep).SetBlockName("trapdoor").DisableStats().IgnoreMetaUpdates();
    public readonly int Id;
    public readonly Material Material;
    public Box BoundingBox;
    public float Hardness;
    public float ParticleFallSpeedModifier;
    public float Resistance;
    public bool ShouldTrackStatistics;
    public float Slipperiness;
    public BlockSoundGroup SoundGroup;
    public int TextureId;
    public virtual bool IsOpaque => true;
    public virtual int TickRate => 10;
    public virtual int DroppedItemCount => 1;
    public virtual bool CanEmitRedstonePower { get; } = false;
    public string? BlockName { get; private set; }
    public virtual bool IsFullCube { get; } = true;
    public virtual PistonBehavior PistonBehavior => Material.PistonBehavior;

    static Block()
    {
        Item.ITEMS[Wool.Id] = new ItemCloth(Wool.Id - 256).setItemName("cloth");
        Item.ITEMS[Log.Id] = new ItemLog(Log.Id - 256).setItemName("log");
        Item.ITEMS[Slab.Id] = new ItemSlab(Slab.Id - 256).setItemName("stoneSlab");
        Item.ITEMS[Sapling.Id] = new ItemSapling(Sapling.Id - 256).setItemName("sapling");
        Item.ITEMS[Leaves.Id] = new ItemLeaves(Leaves.Id - 256).setItemName("leaves");
        Item.ITEMS[Piston.Id] = new ItemPiston(Piston.Id - 256);
        Item.ITEMS[StickyPiston.Id] = new ItemPiston(StickyPiston.Id - 256);

        for (int blockId = 0; blockId < 256; ++blockId)
        {
            if (Blocks[blockId] != null && Item.ITEMS[blockId] == null)
            {
                Item.ITEMS[blockId] = new ItemBlock(blockId - 256);
                Blocks[blockId]?.Init();
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
        Id = id;
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        BlocksOpaque[id] = IsOpaque;
        BlockLightOpacity[id] = IsOpaque ? 255 : 0;
        BlocksAllowVision[id] = !material.BlocksVision;
        BlocksWithEntity[id] = false;
    }

    protected Block(int id, int textureId, Material material) : this(id, material) => TextureId = textureId;

    protected Block IgnoreMetaUpdates()
    {
        BlocksIgnoreMetaUpdate[Id] = true;
        return this;
    }

    protected virtual void Init()
    {
    }

    protected Block SetSoundGroup(BlockSoundGroup soundGroup)
    {
        SoundGroup = soundGroup;
        return this;
    }

    protected Block SetOpacity(int opacity)
    {
        BlockLightOpacity[Id] = opacity;
        return this;
    }

    protected Block SetLuminance(float fractionalValue)
    {
        BlocksLightLuminance[Id] = (int)(15.0F * fractionalValue);
        return this;
    }

    protected Block SetResistance(float resistance)
    {
        Resistance = resistance * 3.0F;
        return this;
    }

    public virtual BlockRendererType GetRenderType() => BlockRendererType.Standard;

    protected Block SetHardness(float hardness)
    {
        Hardness = hardness;
        if (Resistance < hardness * 5.0F)
        {
            Resistance = hardness * 5.0F;
        }

        return this;
    }

    protected Block SetUnbreakable()
    {
        SetHardness(-1.0F);
        return this;
    }

    public float GetHardness() => Hardness;

    protected Block SetTickRandomly(bool tickRandomly)
    {
        BlocksRandomTick[Id] = tickRandomly;
        return this;
    }

    public void SetBoundingBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ) => BoundingBox = new Box(minX, minY, minZ, maxX, maxY, maxZ);

    public virtual float GetLuminance(ILightProvider? lighting, int x, int y, int z)
    {
        if (lighting != null)
        {
            return lighting.GetNaturalBrightness(x, y, z, BlocksLightLuminance[Id]);
        }

        int baseLum = BlocksLightLuminance[Id];
        return baseLum > 0 ? baseLum / 15.0f : 1.0f;
    }

    [Obsolete("Use IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side) instead.")]
    public virtual bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side) => IsSideVisible(iBlockReader, x, y, z, side.ToSide());

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

    protected virtual bool IsSolidFace(IBlockReader iBlockReader, int x, int y, int z, int face) => iBlockReader.GetMaterial(x, y, z).IsSolid;

    public virtual int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, int side) => GetTextureId(iBlockReader, x, y, z, side.ToSide());
    public virtual int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side) => GetTexture(side, iBlockReader.GetBlockMeta(x, y, z));

    [Obsolete("Use GetTexture(Side side, int meta) or GetTexture(Side side)")]
    public virtual int GetTexture(int side, int meta) => GetTexture(side);

    public virtual int GetTexture(Side side, int meta) => GetTexture(side);

    public virtual int GetTexture(int side) => GetTexture(side.ToSide());
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


    public virtual bool HasCollision(int meta, bool allowLiquids) => HasCollision();

    public virtual bool HasCollision() => true;


    public virtual void RandomDisplayTick(OnTickEvent e)
    {
    }

    public virtual void NeighborUpdate(OnTickEvent e)
    {
    }


    public virtual int GetDroppedItemId(int blockMeta) => Id;

    public float GetHardness(EntityPlayer player) => Hardness < 0.0F ? 0.0F : !player.canHarvest(this) ? 1.0F / Hardness / 100.0F : player.getBlockBreakingSpeed(this) / Hardness / 30.0F;

    public virtual void DropStacks(OnDropEvent ctx)
    {
        if (ctx.World.IsRemote || !ctx.World.Rules.GetBool(DefaultRules.DoTileDrops)) return;

        int dropCount = DroppedItemCount;

        for (int attempt = 0; attempt < dropCount; ++attempt)
        {
            if (!(Random.Shared.NextSingle() <= ctx.Luck))
            {
                continue;
            }

            int itemId = GetDroppedItemId(ctx.Meta);
            if (itemId > 0)
            {
                DropStack(ctx.World, ctx.X, ctx.Y, ctx.Z, new ItemStack(itemId, 1, GetDroppedItemMeta(ctx.Meta)));
            }
        }
    }

    protected static void DropStack(IWorldContext world, int x, int y, int z, ItemStack itemStack)
    {
        if (world.IsRemote || !world.Rules.GetBool(DefaultRules.DoTileDrops)) return;

        const float spreadFactor = 0.7F;
        double offsetX = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
        double offsetY = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
        double offsetZ = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5D;
        world.SpawnItemDrop(x + offsetX, y + offsetY, z + offsetZ, itemStack);
    }

    protected virtual int GetDroppedItemMeta(int blockMeta) => 0;

    public virtual float GetBlastResistance(Entity _) => Resistance / 5.0F;

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

    public virtual int GetRenderLayer() => 0;

    public virtual bool CanPlaceAt(CanPlaceAtContext evt)
    {
        int blockId = evt.World.Reader.GetBlockId(evt.X, evt.Y, evt.Z);
        return blockId == 0 || Blocks[blockId]!.Material.IsReplaceable;
    }

    public virtual void OnTick(OnTickEvent e)
    {
    }

    public virtual void OnMetadataChange(OnMetadataChangeEvent ctx)
    {
    }

    public virtual Vec3D ApplyVelocity(OnApplyVelocityEvent @event) => Vec3D.Zero;

    public void UpdateBoundingBox(IBlockReader blockReader, int x, int y, int z) => UpdateBoundingBox(blockReader, null, x, y, z);

    public virtual void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
    }

    public virtual int GetColor(int meta) => 0xFFFFFF;

    public virtual int GetColorForFace(int meta, Side face) => GetColor(meta);

    public virtual int GetColorMultiplier(IBlockReader reader, int x, int y, int z) => 0xFFFFFF;

    public virtual int GetColorMultiplier(IBlockReader reader, int x, int y, int z, int knownMeta) => GetColorMultiplier(reader, x, y, z);

    public virtual bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side) => false;


    public virtual bool IsFlammable(IBlockReader iBlockReader, int x, int y, int z) => false;


    public virtual bool IsStrongPoweringSide(IBlockReader reader, int x, int y, int z, int side) => false;

    public virtual void SetupRenderBoundingBox()
    {
    }

    public virtual void OnPlaced(OnPlacedEvent e)
    {
    }

    public virtual bool CanGrow(OnTickEvent ctx) => true;

    protected Block SetBlockName(string name)
    {
        BlockName = $"tile.{name}";
        return this;
    }

    public string TranslateBlockName() => StatCollector.TranslateToLocal($"{BlockName}.name");


    public bool GetEnableStats() => ShouldTrackStatistics;

    protected Block DisableStats()
    {
        ShouldTrackStatistics = false;
        return this;
    }

    public virtual void OnBreak(OnBreakEvent e)
    {
    }

    public virtual void OnDestroyedByExplosion(OnDestroyedByExplosionEvent @event)
    {
    }

    public virtual bool OnUse(OnUseEvent _) => false;

    public virtual void OnSteppedOn(OnEntityStepEvent @event)
    {
    }

    public virtual void OnBlockBreakStart(OnBlockBreakStartEvent @event)
    {
    }

    public virtual void OnEntityCollision(OnEntityCollisionEvent @event)
    {
    }

    public virtual void OnAfterBreak(OnAfterBreakEvent ctx)
    {
        ctx.Player.increaseStat(Stats.Stats.MineBlockStatArray[Id], 1);
        DropStacks(new OnDropEvent(ctx.World, ctx.X, ctx.Y, ctx.Z, ctx.Meta));
    }

    public virtual void OnBlockAction(OnBlockActionEvent ctx)
    {
    }
}
