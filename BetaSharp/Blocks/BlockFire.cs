using BetaSharp.Blocks.Materials;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockFire : Block
{
    private readonly int[] _burnChances = new int[256];
    private readonly int[] _spreadChances = new int[256];

    public BlockFire(int id, int textureId) : base(id, textureId, Material.Fire)
    {
        setTickRandomly(true);
    }

    protected override void init()
    {
        registerFlammableBlock(Block.Planks.id, 5, 20);
        registerFlammableBlock(Block.Fence.id, 5, 20);
        registerFlammableBlock(Block.WoodenStairs.id, 5, 20);
        registerFlammableBlock(Block.Log.id, 5, 5);
        registerFlammableBlock(Block.Leaves.id, 30, 60);
        registerFlammableBlock(Block.Bookshelf.id, 30, 20);
        registerFlammableBlock(Block.TNT.id, 15, 100);
        registerFlammableBlock(Block.Grass.id, 60, 100);
        registerFlammableBlock(Block.Wool.id, 30, 60);
    }

    private void registerFlammableBlock(int block, int burnChange, int spreadChance)
    {
        _burnChances[block] = burnChange;
        _spreadChances[block] = spreadChance;
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        return null;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Fire;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override int getTickRate()
    {
        return 40;
    }

    public override void onTick(OnTickContext ctx)
    {
        if (!ctx.WorldView.Rules.GetBool(DefaultRules.DoFireTick))
        {
            return;
        }

        bool isOnNetherrack = ctx.WorldView.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z) == Block.Netherrack.id;
        if (!canPlaceAt(ctx.WorldView, ctx.X, ctx.Y, ctx.Z))
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }

        if (isOnNetherrack || !ctx.WorldView.isRaining() || !ctx.WorldView.isRaining(ctx.X, ctx.Y, ctx.Z) && !ctx.WorldView.isRaining(ctx.X - 1, ctx.Y, ctx.Z) && !ctx.WorldView.isRaining(ctx.X + 1, ctx.Y, ctx.Z) && !ctx.WorldView.isRaining(ctx.X, ctx.Y, ctx.Z - 1) && !ctx.WorldView.isRaining(ctx.X, ctx.Y, ctx.Z + 1))
        {
            int fireAge = ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
            if (fireAge < 15)
            {
                ctx.WorldWrite.SetBlockMetaWithoutNotifyingNeighbors(ctx.X, ctx.Y, ctx.Z, fireAge + ctx.Random.NextInt(3) / 2);
            }

            ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
            if (!isOnNetherrack && !areBlocksAroundFlammable(ctx.WorldView, ctx.X, ctx.Y, ctx.Z))
            {
                if (!ctx.WorldView.shouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) || fireAge > 3)
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }

            }
            else if (!isOnNetherrack && !isFlammable(ctx.WorldView, ctx.X, ctx.Y - 1, ctx.Z) && fireAge == 15 && ctx.Random.NextInt(4) == 0)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
            else
            {
                trySpreadingFire(ctx.WorldView, ctx.X + 1, ctx.Y, ctx.Z, 300, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldView, ctx.X - 1, ctx.Y, ctx.Z, 300, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldView, ctx.X, ctx.Y - 1, ctx.Z, 250, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldView, ctx.X, ctx.Y + 1, ctx.Z, 250, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldView, ctx.X, ctx.Y, ctx.Z - 1, 300, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldView, ctx.X, ctx.Y, ctx.Z + 1, 300, ctx.Random, fireAge);

                for (int checkX = ctx.X - 1; checkX <= ctx.X + 1; ++checkX)
                {
                    for (int checkY = ctx.Z - 1; checkY <= ctx.Z + 1; ++checkY)
                    {
                        for (int checkZ = ctx.Y - 1; checkZ <= ctx.Y + 4; ++checkZ)
                        {
                            if (checkX != ctx.X || checkZ != ctx.Y || checkY != ctx.Z)
                            {
                                int spreadDifficulty = 100;
                                if (checkZ > ctx.Y + 1)
                                {
                                    spreadDifficulty += (checkZ - (ctx.Y + 1)) * 100;
                                }

                                int burnChance = getBurnChance(ctx.WorldView, checkX, checkZ, checkY);
                                if (burnChance > 0)
                                {
                                    int var13 = (burnChance + 40) / (fireAge + 30);
                                    if (var13 > 0 && ctx.Random.NextInt(spreadDifficulty) <= var13 && (!ctx.WorldView.isRaining() || !ctx.WorldView.isRaining(checkX, checkZ, checkY)) && !ctx.WorldView.isRaining(checkX - 1, checkZ, z) && !ctx.WorldView.isRaining(checkX + 1, checkZ, checkY) && !ctx.WorldView.isRaining(checkX, checkZ, checkY - 1) && !ctx.WorldView.isRaining(checkX, checkZ, checkY + 1))
                                    {
                                        int spreadChance = fireAge + ctx.Random.NextInt(5) / 4;
                                        if (spreadChance > 15)
                                        {
                                            spreadChance = 15;
                                        }

                                        ctx.WorldWrite.SetBlock(checkX, checkZ, checkY, id, spreadChance);
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }
        else
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
    }

    private void trySpreadingFire(World world, int x, int y, int z, int spreadFactor, JavaRandom random, int currentAge)
    {
        int targetSpreadChance = _spreadChances[world.getBlockId(x, y, z)];
        if (random.NextInt(spreadFactor) < targetSpreadChance)
        {
            bool isTnt = world.getBlockId(x, y, z) == Block.TNT.id;
            if (random.NextInt(currentAge + 10) < 5 && !world.isRaining(x, y, z))
            {
                int newFireAge = currentAge + random.NextInt(5) / 4;
                if (newFireAge > 15)
                {
                    newFireAge = 15;
                }

                world.setBlock(x, y, z, id, newFireAge);
            }
            else
            {
                world.setBlock(x, y, z, 0);
            }

            if (isTnt)
            {
                Block.TNT.onMetadataChange(world, x, y, z, 1);
            }
        }

    }

    private bool areBlocksAroundFlammable(World world, int x, int y, int z)
    {
        return isFlammable(world.BlocksView, x + 1, y, z) ? true : (isFlammable(world.BlocksView, x - 1, y, z) ? true : (isFlammable(world.BlocksView, x, y - 1, z) ? true : (isFlammable(world.BlocksView, x, y + 1, z) ? true : (isFlammable(world.BlocksView, x, y, z - 1) ? true : isFlammable(world.BlocksView, x, y, z + 1)))));
    }

    private int getBurnChance(World world, int x, int y, int z)
    {
        sbyte initialMax = 0;
        if (!world.isAir(x, y, z))
        {
            return 0;
        }
        else
        {
            int maxChance = getBurnChance(world, x + 1, y, z, initialMax);
            maxChance = getBurnChance(world, x - 1, y, z, maxChance);
            maxChance = getBurnChance(world, x, y - 1, z, maxChance);
            maxChance = getBurnChance(world, x, y + 1, z, maxChance);
            maxChance = getBurnChance(world, x, y, z - 1, maxChance);
            maxChance = getBurnChance(world, x, y, z + 1, maxChance);
            return maxChance;
        }
    }

    public override bool hasCollision()
    {
        return false;
    }

    public override bool isFlammable(IBlockReader iBlockReader, int x, int y, int z)
    {
        return _burnChances[iBlockReader.GetBlockId(x, y, z)] > 0;
    }

    public int getBurnChance(World world, int x, int y, int z, int currentChance)
    {
        int blockBurnChance = _burnChances[world.getBlockId(x, y, z)];
        return blockBurnChance > currentChance ? blockBurnChance : currentChance;
    }

    public override bool canPlaceAt(WorldBlockView world, int x, int y, int z)
    {
        return world.shouldSuffocate(x, y - 1, z) || areBlocksAroundFlammable(world, x, y, z);
    }

    public override void neighborUpdate(WorldBlockView world, int x, int y, int z, int id)
    {
        if (!world.shouldSuffocate(x, y - 1, z) && !areBlocksAroundFlammable(world, x, y, z))
        {
            world.setBlock(x, y, z, 0);
        }
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        if (world.getBlockId(x, y - 1, z) != Block.Obsidian.id || !Block.NetherPortal.create(world, x, y, z))
        {
            if (!world.shouldSuffocate(x, y - 1, z) && !areBlocksAroundFlammable(world, x, y, z))
            {
                world.setBlock(x, y, z, 0);
            }
            else
            {
                world.ScheduleBlockUpdate(x, y, z, id, getTickRate());
            }
        }
    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (random.NextInt(24) == 0)
        {
            world.playSound((double)((float)x + 0.5F), (double)((float)y + 0.5F), (double)((float)z + 0.5F), "fire.fire", 1.0F + random.NextFloat(), random.NextFloat() * 0.7F + 0.3F);
        }

        int particleIndex;
        float particleX;
        float particleY;
        float particleZ;
        if (!world.shouldSuffocate(x, y - 1, z) && !Block.Fire.isFlammable(world.BlocksView, x, y - 1, z))
        {
            if (Block.Fire.isFlammable(world.BlocksView, x - 1, y, z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = (float)x + random.NextFloat() * 0.1F;
                    particleY = (float)y + random.NextFloat();
                    particleZ = (float)z + random.NextFloat();
                    world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.isFlammable(world.BlocksView, x + 1, y, z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = (float)(x + 1) - random.NextFloat() * 0.1F;
                    particleY = (float)y + random.NextFloat();
                    particleZ = (float)z + random.NextFloat();
                    world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Block.Fire.isFlammable(world.BlocksView, x, y, z - 1))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = (float)x + random.NextFloat();
                    particleY = (float)y + random.NextFloat();
                    particleZ = (float)z + random.NextFloat() * 0.1F;
                    world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Block.Fire.isFlammable(world.BlocksView, x, y, z + 1))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = (float)x + random.NextFloat();
                    particleY = (float)y + random.NextFloat();
                    particleZ = (float)(z + 1) - random.NextFloat() * 0.1F;
                    world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Block.Fire.isFlammable(world.BlocksView, x, y + 1, z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = (float)x + random.NextFloat();
                    particleY = (float)(y + 1) - random.NextFloat() * 0.1F;
                    particleZ = (float)z + random.NextFloat();
                    world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
                }
            }
        }
        else
        {
            for (particleIndex = 0; particleIndex < 3; ++particleIndex)
            {
                particleX = (float)x + random.NextFloat();
                particleY = (float)y + random.NextFloat() * 0.5F + 0.5F;
                particleZ = (float)z + random.NextFloat();
                world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
            }
        }

    }
}
