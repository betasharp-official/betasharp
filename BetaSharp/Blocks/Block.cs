using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Rules;
using BetaSharp.Stats;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class Block
{
    public static readonly BlockSoundGroup SoundPowderFootstep = new("stone", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundWoodFootstep = new("wood", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundGravelFootstep = new("gravel", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundGrassFootstep = new("grass", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundStoneFootstep = new("stone", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundMetalFootstep = new("stone", 1.0F, 1.5F);
    public static readonly BlockSoundGroup SoundGlassFootstep = new ("stone", 1.0F, 1.0F, "random.glass");
    public static readonly BlockSoundGroup SoundClothFootstep = new("cloth", 1.0F, 1.0F);
    public static readonly BlockSoundGroup SoundSandFootstep = new ("sand", 1.0F, 1.0F, "step.gravel");

    public static readonly Block[] Blocks = new Block[256];
    public static readonly bool[] BlocksRandomTick = new bool[256];
    public static readonly bool[] BlocksOpaque = new bool[256];
    public static readonly bool[] BlocksWithEntity = new bool[256];
    public static readonly int[] BlockLightOpacity = new int[256];
    public static readonly bool[] BlocksAllowVision = new bool[256];
    public static readonly int[] BlocksLightLuminance = new int[256];
    public static readonly bool[] BlocksIngoreMetaUpdate = new bool[256];

    public static readonly Block Stone = new BlockStone(1, 1).setHardness(1.5F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stone");
    public static readonly BlockGrass GrassBlock = (BlockGrass)new BlockGrass(2).setHardness(0.6F).setSoundGroup(SoundGrassFootstep).setBlockName("grass");
    public static readonly Block Dirt = new BlockDirt(3, 2).setHardness(0.5F).setSoundGroup(SoundGravelFootstep).setBlockName("dirt");
    public static readonly Block Cobblestone = new Block(4, 16, Material.Stone).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stonebrick");
    public static readonly Block Planks = new Block(5, 4, Material.Wood).setHardness(2.0F).setResistance(5.0F).setSoundGroup(SoundWoodFootstep).setBlockName("wood").ignoreMetaUpdates();
    public static readonly Block Sapling = new BlockSapling(6, 15).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("sapling").ignoreMetaUpdates();
    public static readonly Block Bedrock = new Block(7, 17, Material.Stone).setUnbreakable().setResistance(6000000.0F).setSoundGroup(SoundStoneFootstep).setBlockName("bedrock").disableStats();
    public static readonly Block FlowingWater = new BlockFlowing(8, Material.Water).setHardness(100.0F).setOpacity(3).setBlockName("water").disableStats().ignoreMetaUpdates();
    public static readonly Block Water = new BlockStationary(9, Material.Water).setHardness(100.0F).setOpacity(3).setBlockName("water").disableStats().ignoreMetaUpdates();
    public static readonly Block FlowingLava = new BlockFlowing(10, Material.Lava).setHardness(0.0F).setLuminance(1.0F).setOpacity(255).setBlockName("lava").disableStats().ignoreMetaUpdates();
    public static readonly Block Lava = new BlockStationary(11, Material.Lava).setHardness(100.0F).setLuminance(1.0F).setOpacity(255).setBlockName("lava").disableStats().ignoreMetaUpdates();
    public static readonly Block Sand = new BlockSand(12, 18).setHardness(0.5F).setSoundGroup(SoundSandFootstep).setBlockName("sand");
    public static readonly Block Gravel = new BlockGravel(13, 19).setHardness(0.6F).setSoundGroup(SoundGravelFootstep).setBlockName("gravel");
    public static readonly Block GoldOre = new BlockOre(14, 32).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreGold");
    public static readonly Block IronOre = new BlockOre(15, 33).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreIron");
    public static readonly Block CoalOre = new BlockOre(16, 34).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreCoal");
    public static readonly Block Log = new BlockLog(17).setHardness(2.0F).setSoundGroup(SoundWoodFootstep).setBlockName("log").ignoreMetaUpdates();
    public static readonly BlockLeaves Leaves = (BlockLeaves)new BlockLeaves(18, 52).setHardness(0.2F).setOpacity(1).setSoundGroup(SoundGrassFootstep).setBlockName("leaves").disableStats().ignoreMetaUpdates();
    public static readonly Block Sponge = new BlockSponge(19).setHardness(0.6F).setSoundGroup(SoundGrassFootstep).setBlockName("sponge");
    public static readonly Block Glass = new BlockGlass(20, 49, Material.Glass, false).setHardness(0.3F).setSoundGroup(SoundGlassFootstep).setBlockName("glass");
    public static readonly Block LapisOre = new BlockOre(21, 160).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreLapis");
    public static readonly Block LapisBlock = new Block(22, 144, Material.Stone).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("blockLapis");
    public static readonly Block Dispenser = new BlockDispenser(23).setHardness(3.5F).setSoundGroup(SoundStoneFootstep).setBlockName("dispenser").ignoreMetaUpdates();
    public static readonly Block Sandstone = new BlockSandStone(24).setSoundGroup(SoundStoneFootstep).setHardness(0.8F).setBlockName("sandStone");
    public static readonly Block Noteblock = new BlockNote(25).setHardness(0.8F).setBlockName("musicBlock").ignoreMetaUpdates();
    public static readonly Block Bed = new BlockBed(26).setHardness(0.2F).setBlockName("bed").disableStats().ignoreMetaUpdates();
    public static readonly Block PoweredRail = new BlockRail(27, 179, true).setHardness(0.7F).setSoundGroup(SoundMetalFootstep).setBlockName("goldenRail").ignoreMetaUpdates();
    public static readonly Block DetectorRail = new BlockDetectorRail(28, 195).setHardness(0.7F).setSoundGroup(SoundMetalFootstep).setBlockName("detectorRail").ignoreMetaUpdates();
    public static readonly Block StickyPiston = new BlockPistonBase(29, 106, true).setBlockName("pistonStickyBase").ignoreMetaUpdates();
    public static readonly Block Cobweb = new BlockWeb(30, 11).setOpacity(1).setHardness(4.0F).setBlockName("web");
    public static readonly BlockTallGrass Grass = (BlockTallGrass)new BlockTallGrass(31, 39).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("tallgrass");
    public static readonly BlockDeadBush DeadBush = (BlockDeadBush)new BlockDeadBush(32, 55).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("deadbush");
    public static readonly Block Piston = new BlockPistonBase(33, 107, false).setBlockName("pistonBase").ignoreMetaUpdates();
    public static readonly BlockPistonExtension PistonHead = (BlockPistonExtension)new BlockPistonExtension(34, 107).ignoreMetaUpdates();
    public static readonly Block Wool = new BlockCloth().setHardness(0.8F).setSoundGroup(SoundClothFootstep).setBlockName("cloth").ignoreMetaUpdates();
    public static readonly BlockPistonMoving MovingPiston = new BlockPistonMoving(36);
    public static readonly BlockPlant Dandelion = (BlockPlant)new BlockPlant(37, 13).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("flower");
    public static readonly BlockPlant Rose = (BlockPlant)new BlockPlant(38, 12).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("rose");
    public static readonly BlockPlant BrownMushroom = (BlockPlant)new BlockMushroom(39, 29).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setLuminance(2.0F / 16.0F).setBlockName("mushroom");
    public static readonly BlockPlant RedMushroom = (BlockPlant)new BlockMushroom(40, 28).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("mushroom");
    public static readonly Block GoldBlock = new BlockOreStorage(41, 23).setHardness(3.0F).setResistance(10.0F).setSoundGroup(SoundMetalFootstep).setBlockName("blockGold");
    public static readonly Block IronBlock = new BlockOreStorage(42, 22).setHardness(5.0F).setResistance(10.0F).setSoundGroup(SoundMetalFootstep).setBlockName("blockIron");
    public static readonly Block DoubleSlab = new BlockSlab(43, true).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stoneSlab");
    public static readonly Block Slab = new BlockSlab(44, false).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stoneSlab");
    public static readonly Block Bricks = new Block(45, 7, Material.Stone).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("brick");
    public static readonly Block TNT = new BlockTNT(46, 8).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("tnt");
    public static readonly Block Bookshelf = new BlockBookshelf(47, 35).setHardness(1.5F).setSoundGroup(SoundWoodFootstep).setBlockName("bookshelf");
    public static readonly Block MossyCobblestone = new Block(48, 36, Material.Stone).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("stoneMoss");
    public static readonly Block Obsidian = new BlockObsidian(49, 37).setHardness(10.0F).setResistance(2000.0F).setSoundGroup(SoundStoneFootstep).setBlockName("obsidian");
    public static readonly Block Torch = new BlockTorch(50, 80).setHardness(0.0F).setLuminance(15.0F / 16.0F).setSoundGroup(SoundWoodFootstep).setBlockName("torch").ignoreMetaUpdates();
    public static readonly Block Fire = (BlockFire)new BlockFire(51, 31).setHardness(0.0F).setLuminance(1.0F).setSoundGroup(SoundWoodFootstep).setBlockName("fire").disableStats().ignoreMetaUpdates();
    public static readonly Block Spawner = new BlockMobSpawner(52, 65).setHardness(5.0F).setSoundGroup(SoundMetalFootstep).setBlockName("mobSpawner").disableStats();
    public static readonly Block WoodenStairs = new BlockStairs(53, Planks).setBlockName("stairsWood").ignoreMetaUpdates();
    public static readonly Block Chest = new BlockChest(54).setHardness(2.5F).setSoundGroup(SoundWoodFootstep).setBlockName("chest").ignoreMetaUpdates();
    public static readonly Block RedstoneWire = new BlockRedstoneWire(55, 164).setHardness(0.0F).setSoundGroup(SoundPowderFootstep).setBlockName("redstoneDust").disableStats().ignoreMetaUpdates();
    public static readonly Block DiamondOre = new BlockOre(56, 50).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreDiamond");
    public static readonly Block DiamondBlock = new BlockOreStorage(57, 24).setHardness(5.0F).setResistance(10.0F).setSoundGroup(SoundMetalFootstep).setBlockName("blockDiamond");
    public static readonly Block CraftingTable = new BlockWorkbench(58).setHardness(2.5F).setSoundGroup(SoundWoodFootstep).setBlockName("workbench");
    public static readonly Block Wheat = new BlockCrops(59, 88).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("crops").disableStats().ignoreMetaUpdates();
    public static readonly Block Farmland = new BlockFarmland(60).setHardness(0.6F).setSoundGroup(SoundGravelFootstep).setBlockName("farmland");
    public static readonly Block Furnace = new BlockFurnace(61, false).setHardness(3.5F).setSoundGroup(SoundStoneFootstep).setBlockName("furnace").ignoreMetaUpdates();
    public static readonly Block LitFurnace = new BlockFurnace(62, true).setHardness(3.5F).setSoundGroup(SoundStoneFootstep).setLuminance(14.0F / 16.0F).setBlockName("furnace").ignoreMetaUpdates();
    public static readonly Block Sign = new BlockSign(63, typeof(BlockEntitySign), true).setHardness(1.0F).setSoundGroup(SoundWoodFootstep).setBlockName("sign").disableStats().ignoreMetaUpdates();
    public static readonly Block Door = new BlockDoor(64, Material.Wood).setHardness(3.0F).setSoundGroup(SoundWoodFootstep).setBlockName("doorWood").disableStats().ignoreMetaUpdates();
    public static readonly Block Ladder = new BlockLadder(65, 83).setHardness(0.4F).setSoundGroup(SoundWoodFootstep).setBlockName("ladder").ignoreMetaUpdates();
    public static readonly Block Rail = new BlockRail(66, 128, false).setHardness(0.7F).setSoundGroup(SoundMetalFootstep).setBlockName("rail").ignoreMetaUpdates();
    public static readonly Block CobblestoneStairs = new BlockStairs(67, Cobblestone).setBlockName("stairsStone").ignoreMetaUpdates();
    public static readonly Block WallSign = new BlockSign(68, typeof(BlockEntitySign), false).setHardness(1.0F).setSoundGroup(SoundWoodFootstep).setBlockName("sign").disableStats().ignoreMetaUpdates();
    public static readonly Block Lever = new BlockLever(69, 96).setHardness(0.5F).setSoundGroup(SoundWoodFootstep).setBlockName("lever").ignoreMetaUpdates();
    public static readonly Block StonePressurePlate = new BlockPressurePlate(70, Stone.textureId, PressurePlateActiviationRule.MOBS, Material.Stone).setHardness(0.5F).setSoundGroup(SoundStoneFootstep).setBlockName("pressurePlate").ignoreMetaUpdates();
    public static readonly Block IronDoor = new BlockDoor(71, Material.Metal).setHardness(5.0F).setSoundGroup(SoundMetalFootstep).setBlockName("doorIron").disableStats().ignoreMetaUpdates();
    public static readonly Block WoodenPressurePlate = new BlockPressurePlate(72, Planks.textureId, PressurePlateActiviationRule.EVERYTHING, Material.Wood).setHardness(0.5F).setSoundGroup(SoundWoodFootstep).setBlockName("pressurePlate").ignoreMetaUpdates();
    public static readonly Block RedstoneOre = new BlockRedstoneOre(73, 51, false).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreRedstone").ignoreMetaUpdates();
    public static readonly Block LitRedstoneOre = new BlockRedstoneOre(74, 51, true).setLuminance(10.0F / 16.0F).setHardness(3.0F).setResistance(5.0F).setSoundGroup(SoundStoneFootstep).setBlockName("oreRedstone").ignoreMetaUpdates();
    public static readonly Block RedstoneTorch = new BlockRedstoneTorch(75, 115, false).setHardness(0.0F).setSoundGroup(SoundWoodFootstep).setBlockName("notGate").ignoreMetaUpdates();
    public static readonly Block LitRedstoneTorch = new BlockRedstoneTorch(76, 99, true).setHardness(0.0F).setLuminance(0.5F).setSoundGroup(SoundWoodFootstep).setBlockName("notGate").ignoreMetaUpdates();
    public static readonly Block Button = new BlockButton(77, Stone.textureId).setHardness(0.5F).setSoundGroup(SoundStoneFootstep).setBlockName("button").ignoreMetaUpdates();
    public static readonly Block Snow = new BlockSnow(78, 66).setHardness(0.1F).setSoundGroup(SoundClothFootstep).setBlockName("snow");
    public static readonly Block Ice = new BlockIce(79, 67).setHardness(0.5F).setOpacity(3).setSoundGroup(SoundGlassFootstep).setBlockName("ice");
    public static readonly Block SnowBlock = new BlockSnowBlock(80, 66).setHardness(0.2F).setSoundGroup(SoundClothFootstep).setBlockName("snow");
    public static readonly Block Cactus = new BlockCactus(81, 70).setHardness(0.4F).setSoundGroup(SoundClothFootstep).setBlockName("cactus");
    public static readonly Block Clay = new BlockClay(82, 72).setHardness(0.6F).setSoundGroup(SoundGravelFootstep).setBlockName("clay");
    public static readonly Block SugarCane = new BlockReed(83, 73).setHardness(0.0F).setSoundGroup(SoundGrassFootstep).setBlockName("reeds").disableStats();
    public static readonly Block Jukebox = new BlockJukeBox(84, 74).setHardness(2.0F).setResistance(10.0F).setSoundGroup(SoundStoneFootstep).setBlockName("jukebox").ignoreMetaUpdates();
    public static readonly Block Fence = new BlockFence(85, 4).setHardness(2.0F).setResistance(5.0F).setSoundGroup(SoundWoodFootstep).setBlockName("fence").ignoreMetaUpdates();
    public static readonly Block Pumpkin = new BlockPumpkin(86, 102, false).setHardness(1.0F).setSoundGroup(SoundWoodFootstep).setBlockName("pumpkin").ignoreMetaUpdates();
    public static readonly Block Netherrack = new BlockNetherrack(87, 103).setHardness(0.4F).setSoundGroup(SoundStoneFootstep).setBlockName("hellrock");
    public static readonly Block Soulsand = new BlockSoulSand(88, 104).setHardness(0.5F).setSoundGroup(SoundSandFootstep).setBlockName("hellsand");
    public static readonly Block Glowstone = new BlockGlowstone(89, 105, Material.Stone).setHardness(0.3F).setSoundGroup(SoundGlassFootstep).setLuminance(1.0F).setBlockName("lightgem");
    public static readonly BlockPortal NetherPortal = (BlockPortal)new BlockPortal(90, 14).setHardness(-1.0F).setSoundGroup(SoundGlassFootstep).setLuminance(12.0F / 16.0F).setBlockName("portal");
    public static readonly Block JackLantern = new BlockPumpkin(91, 102, true).setHardness(1.0F).setSoundGroup(SoundWoodFootstep).setLuminance(1.0F).setBlockName("litpumpkin").ignoreMetaUpdates();
    public static readonly Block Cake = new BlockCake(92, 121).setHardness(0.5F).setSoundGroup(SoundClothFootstep).setBlockName("cake").disableStats().ignoreMetaUpdates();
    public static readonly Block Repeater = new BlockRedstoneRepeater(93, false).setHardness(0.0F).setSoundGroup(SoundWoodFootstep).setBlockName("diode").disableStats().ignoreMetaUpdates();
    public static readonly Block PoweredRepeater = new BlockRedstoneRepeater(94, true).setHardness(0.0F).setLuminance(10.0F / 16.0F).setSoundGroup(SoundWoodFootstep).setBlockName("diode").disableStats().ignoreMetaUpdates();
    public static readonly Block Trapdoor = new BlockTrapDoor(96, Material.Wood).setHardness(3.0F).setSoundGroup(SoundWoodFootstep).setBlockName("trapdoor").disableStats().ignoreMetaUpdates();
    public int textureId;
    public readonly int id;
    public float hardness;
    public float resistance;
    protected bool shouldTrackStatistics;
    public Box BoundingBox;
    public BlockSoundGroup soundGroup;
    public float particleFallSpeedModifier;
    public readonly Material material;
    public float slipperiness;
    private string? blockName;

    protected Block(int id, Material material)
    {
        shouldTrackStatistics = true;
        soundGroup = SoundPowderFootstep;
        particleFallSpeedModifier = 1.0F;
        slipperiness = 0.6F;
        if (Blocks[id] != null)
        {
            throw new ArgumentException("Slot " + id + " is already occupied by " + Blocks[id] + " when adding " + this, nameof(id));
        }
        else
        {
            this.material = material;
            Blocks[id] = this;
            this.id = id;
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
            BlocksOpaque[id] = isOpaque();
            BlockLightOpacity[id] = isOpaque() ? 255 : 0;
            BlocksAllowVision[id] = !material.BlocksVision;
            BlocksWithEntity[id] = false;
        }
    }

    protected Block ignoreMetaUpdates()
    {
        BlocksIngoreMetaUpdate[id] = true;
        return this;
    }

    protected virtual void init()
    {
    }

    protected Block(int id, int textureId, Material material) : this(id, material)
    {
        this.textureId = textureId;
    }

    protected Block setSoundGroup(BlockSoundGroup soundGroup)
    {
        this.soundGroup = soundGroup;
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

    public virtual bool isFullCube()
    {
        return true;
    }

    public virtual BlockRendererType getRenderType()
    {
        return BlockRendererType.Standard;
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

    public float getHardness()
    {
        return hardness;
    }

    protected Block setTickRandomly(bool tickRandomly)
    {
        BlocksRandomTick[id] = tickRandomly;
        return this;
    }

    public void setBoundingBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        BoundingBox = new Box(minX, minY, minZ, maxX, maxY, maxZ);
    }

    public virtual float getLuminance(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        return iBlockAccess.getNaturalBrightness(x, y, z, BlocksLightLuminance[id]);
    }

    public virtual bool isSideVisible(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        var minX = BoundingBox.MinX;
        var minY = BoundingBox.MinY;
        var minZ = BoundingBox.MinZ;
        var maxX = BoundingBox.MaxX;
        var maxY = BoundingBox.MaxY;
        var maxZ = BoundingBox.MaxZ;
        return side == 0 && minY > 0.0D ? true : (side == 1 && maxY < 1.0D ? true : (side == 2 && minZ > 0.0D ? true : (side == 3 && maxZ < 1.0D ? true : (side == 4 && minX > 0.0D ? true : (side == 5 && maxX < 1.0D ? true : !iBlockAccess.isOpaque(x, y, z))))));
    }

    public virtual bool isSolidFace(IBlockAccess iBlockAccess, int x, int y, int z, int face)
    {
        return iBlockAccess.getMaterial(x, y, z).IsSolid;
    }

    public virtual int getTextureId(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        return getTexture(side, iBlockAccess.getBlockMeta(x, y, z));
    }

    public virtual int getTexture(int side, int meta)
    {
        return getTexture(side);
    }

    public virtual int getTexture(int side)
    {
        return textureId;
    }

    public virtual Box getBoundingBox(World world, int x, int y, int z)
    {
        return BoundingBox.Offset(x, y, z);
    }

    public virtual void addIntersectingBoundingBox(World world, int x, int y, int z, Box box, List<Box> boxes)
    {
        Box? collisionBox = getCollisionShape(world, x, y, z);
        if (collisionBox != null && box.Intersects(collisionBox.Value))
        {
            boxes.Add(collisionBox.Value);
        }
    }

    public virtual Box? getCollisionShape(World world, int x, int y, int z)
    {
        return BoundingBox.Offset(x, y, z);
    }

    public virtual bool isOpaque()
    {
        return true;
    }

    public virtual bool hasCollision(int meta, bool allowLiquids)
    {
        return hasCollision();
    }

    public virtual bool hasCollision()
    {
        return true;
    }

    public virtual void onTick(World world, int x, int y, int z, JavaRandom random)
    {
    }

    public virtual void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
    }

    public virtual void onMetadataChange(World world, int x, int y, int z, int meta)
    {
    }

    public virtual void neighborUpdate(World world, int x, int y, int z, int id)
    {
    }

    public virtual int getTickRate()
    {
        return 10;
    }

    public virtual void onPlaced(World world, int x, int y, int z)
    {
    }

    public virtual void onBreak(World world, int x, int y, int z)
    {
    }

    public virtual int getDroppedItemCount(JavaRandom random)
    {
        return 1;
    }

    public virtual int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return id;
    }

    public float getHardness(EntityPlayer player)
    {
        return hardness < 0.0F ? 0.0F : (!player.canHarvest(this) ? 1.0F / hardness / 100.0F : player.getBlockBreakingSpeed(this) / hardness / 30.0F);
    }

    public void dropStacks(World world, int x, int y, int z, int meta)
    {
        dropStacks(world, x, y, z, meta, 1.0F);
    }

    public virtual void dropStacks(World world, int x, int y, int z, int meta, float luck)
    {
        if (!world.isRemote && world.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            int dropCount = getDroppedItemCount(world.random);

            for (int attempt = 0; attempt < dropCount; ++attempt)
            {
                if (world.random.NextFloat() <= luck)
                {
                    int itemId = getDroppedItemId(meta, world.random);
                    if (itemId > 0)
                    {
                        dropStack(world, x, y, z, new ItemStack(itemId, 1, getDroppedItemMeta(meta)));
                    }
                }
            }

        }
    }

    protected void dropStack(World world, int x, int y, int z, ItemStack itemStack)
    {
        if (!world.isRemote && world.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            float spreadFactor = 0.7F;
            double offsetX = (double)(world.random.NextFloat() * spreadFactor) + (double)(1.0F - spreadFactor) * 0.5D;
            double offsetY = (double)(world.random.NextFloat() * spreadFactor) + (double)(1.0F - spreadFactor) * 0.5D;
            double offsetZ = (double)(world.random.NextFloat() * spreadFactor) + (double)(1.0F - spreadFactor) * 0.5D;
            EntityItem droppedItem = new EntityItem(world, (double)x + offsetX, (double)y + offsetY, (double)z + offsetZ, itemStack);
            droppedItem.delayBeforeCanPickup = 10;
            world.SpawnEntity(droppedItem);
        }
    }

    protected virtual int getDroppedItemMeta(int blockMeta)
    {
        return 0;
    }

    public virtual float getBlastResistance(Entity entity)
    {
        return resistance / 5.0F;
    }

    public virtual HitResult raycast(World world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, x, y, z);
        Vec3D pos = new Vec3D(x, y, z);
        HitResult res = BoundingBox.Raycast(startPos - pos, endPos - pos);
        if (res.Type == HitResultType.MISS) return new HitResult(HitResultType.MISS);
        res.BlockX = x;
        res.BlockY = y;
        res.BlockZ = z;
        res.Pos += pos;
        return res;
    }

    public virtual void onDestroyedByExplosion(World world, int x, int y, int z)
    {
    }

    public virtual int getRenderLayer()
    {
        return 0;
    }

    public virtual bool canPlaceAt(World world, int x, int y, int z, int side)
    {
        return canPlaceAt(world, x, y, z);
    }

    public virtual bool canPlaceAt(World world, int x, int y, int z)
    {
        int blockId = world.getBlockId(x, y, z);
        return blockId == 0 || Blocks[blockId].material.IsReplaceable;
    }

    public virtual bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        return false;
    }

    public virtual void onSteppedOn(World world, int x, int y, int z, Entity entity)
    {
    }

    public virtual void onPlaced(World world, int x, int y, int z, int direction)
    {
    }

    public virtual void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
    }

    public virtual void applyVelocity(World world, int x, int y, int z, Entity entity, Vec3D velocity)
    {
    }

    public virtual void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
    }

    public virtual int getColor(int meta)
    {
        return 0xFFFFFF;
    }

    public virtual int getColorForFace(int meta, int face) => getColor(meta);

    public virtual int getColorMultiplier(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        return 0xFFFFFF;
    }

    public virtual bool isPoweringSide(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        return false;
    }

    public virtual bool canEmitRedstonePower()
    {
        return false;
    }

    public virtual bool isFlammable(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        return false;
    }

    public virtual void onEntityCollision(World world, int x, int y, int z, Entity entity)
    {
    }

    public virtual bool isStrongPoweringSide(World world, int x, int y, int z, int side)
    {
        return false;
    }

    public virtual void setupRenderBoundingBox()
    {
    }

    public virtual void afterBreak(World world, EntityPlayer player, int x, int y, int z, int meta)
    {
        player.increaseStat(Stats.Stats.MineBlockStatArray[id], 1);
        dropStacks(world, x, y, z, meta);
    }

    public virtual bool canGrow(World world, int x, int y, int z)
    {
        return true;
    }

    public virtual void onPlaced(World world, int x, int y, int z, EntityLiving placer)
    {
    }

    public Block setBlockName(string name)
    {
        blockName = "tile." + name;
        return this;
    }

    public string translateBlockName()
    {
        return StatCollector.TranslateToLocal(getBlockName() + ".name");
    }

    public string getBlockName()
    {
        return blockName;
    }

    public virtual void onBlockAction(World world, int x, int y, int z, int data1, int data2)
    {
    }

    public bool getEnableStats()
    {
        return shouldTrackStatistics;
    }

    protected Block disableStats()
    {
        shouldTrackStatistics = false;
        return this;
    }

    public virtual int getPistonBehavior()
    {
        return material.PistonBehavior;
    }

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
}
