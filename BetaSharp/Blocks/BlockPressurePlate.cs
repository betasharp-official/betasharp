using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockPressurePlate : Block
{
    private readonly PressurePlateActiviationRule activationRule;

    public BlockPressurePlate(int id, int textureId, PressurePlateActiviationRule rule, Material material) : base(id, textureId, material)
    {
        activationRule = rule;
        setTickRandomly(true);
        float edgeInset = 1.0F / 16.0F;
        setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, 1 / 32f, 1.0F - edgeInset);
    }

    public override int getTickRate() => 20;

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override bool canPlaceAt(WorldBlockView world, int x, int y, int z) => world.shouldSuffocate(x, y - 1, z);

    public override void onPlaced(World world, int x, int y, int z)
    {
    }

    public override void neighborUpdate(WorldBlockView world, int x, int y, int z, int id)
    {
        bool shouldBreak = false;
        if (!world.shouldSuffocate(x, y - 1, z))
        {
            shouldBreak = true;
        }

        if (shouldBreak)
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }
    }

    public override void onTick(WorldBlockView worldView, int x, int y, int z, JavaRandom random, WorldEventBroadcaster broadcaster, bool isRemote)
    {
        if (!worldView.isRemote)
        {
            if (worldView.getBlockMeta(x, y, z) != 0)
            {
                updatePlateState(worldView, x, y, z);
            }
        }
    }

    public override void onEntityCollision(World world, int x, int y, int z, Entity entity)
    {
        if (!world.isRemote)
        {
            if (world.getBlockMeta(x, y, z) != 1)
            {
                updatePlateState(world, x, y, z);
            }
        }
    }

    private void updatePlateState(World world, int x, int y, int z)
    {
        bool wasPressed = world.getBlockMeta(x, y, z) == 1;
        bool shouldBePressed = false;
        float detectionInset = 2.0F / 16.0F;
        List<Entity> entitiesInBox = null;
        if (activationRule == PressurePlateActiviationRule.EVERYTHING)
        {
            entitiesInBox = world.getEntities(null, new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset));
        }

        if (activationRule == PressurePlateActiviationRule.MOBS)
        {
            entitiesInBox = world.CollectEntitiesOfType<EntityLiving>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset)).Cast<Entity>().ToList();
        }

        if (activationRule == PressurePlateActiviationRule.PLAYERS)
        {
            entitiesInBox = world.CollectEntitiesOfType<EntityPlayer>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset)).Cast<Entity>().ToList();
        }

        if (entitiesInBox.Count > 0)
        {
            shouldBePressed = true;
        }

        if (shouldBePressed && !wasPressed)
        {
            world.setBlockMeta(x, y, z, 1);
            world.notifyNeighbors(x, y, z, id);
            world.notifyNeighbors(x, y - 1, z, id);
            world.setBlocksDirty(x, y, z, x, y, z);
            world.playSound(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, 0.6F);
        }

        if (!shouldBePressed && wasPressed)
        {
            world.setBlockMeta(x, y, z, 0);
            world.notifyNeighbors(x, y, z, id);
            world.notifyNeighbors(x, y - 1, z, id);
            world.setBlocksDirty(x, y, z, x, y, z);
            world.playSound(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, 0.5F);
        }

        if (shouldBePressed)
        {
            world.ScheduleBlockUpdate(x, y, z, id, getTickRate());
        }
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        int plateState = world.getBlockMeta(x, y, z);
        if (plateState > 0)
        {
            world.notifyNeighbors(x, y, z, id);
            world.notifyNeighbors(x, y - 1, z, id);
        }

        base.onBreak(world, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        bool isPressed = iBlockReader.getBlockMeta(x, y, z) == 1;
        float edgeInset = 1.0F / 16.0F;
        if (isPressed)
        {
            setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, 1 / 32f, 1.0F - edgeInset);
        }
        else
        {
            setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, 1.0F / 16.0F, 1.0F - edgeInset);
        }
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) => iBlockReader.getBlockMeta(x, y, z) > 0;

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => world.getBlockMeta(x, y, z) == 0 ? false : side == 1;

    public override bool canEmitRedstonePower() => true;

    public override void setupRenderBoundingBox()
    {
        float halfWidth = 0.5F;
        float halfHeight = 2.0F / 16.0F;
        float halfDepth = 0.5F;
        setBoundingBox(0.5F - halfWidth, 0.5F - halfHeight, 0.5F - halfDepth, 0.5F + halfWidth, 0.5F + halfHeight, 0.5F + halfDepth);
    }

    public override int getPistonBehavior() => 1;
}
