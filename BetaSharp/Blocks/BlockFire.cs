using BetaSharp.Blocks.Materials;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockFire : Block
{
    private readonly int[] _burnChances = new int[256];
    private readonly int[] _spreadChances = new int[256];

    public BlockFire(int id, int textureId) : base(id, textureId, Material.Fire) => setTickRandomly(true);

    protected override void init()
    {
        registerFlammableBlock(Planks.id, 5, 20);
        registerFlammableBlock(Fence.id, 5, 20);
        registerFlammableBlock(WoodenStairs.id, 5, 20);
        registerFlammableBlock(Log.id, 5, 5);
        registerFlammableBlock(Leaves.id, 30, 60);
        registerFlammableBlock(Bookshelf.id, 30, 20);
        registerFlammableBlock(TNT.id, 15, 100);
        registerFlammableBlock(Grass.id, 60, 100);
        registerFlammableBlock(Wool.id, 30, 60);
    }

    private void registerFlammableBlock(int block, int burnChange, int spreadChance)
    {
        _burnChances[block] = burnChange;
        _spreadChances[block] = spreadChance;
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Fire;

    public override int getDroppedItemCount() => 0;

    public override int getTickRate() => 40;

    public override void onTick(OnTickEvt ctx)
    {
        if (!ctx.Rules.GetBool(DefaultRules.DoFireTick))
        {
            return;
        }

        bool isOnNetherrack = ctx.WorldRead.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z) == Netherrack.id;
        if (!canPlaceAt(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z))
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }

        if (isOnNetherrack || !ctx.Environment.IsRaining() || (!ctx.Environment.IsRaining(ctx.X, ctx.Y, ctx.Z) && !ctx.Environment.IsRaining(ctx.X - 1, ctx.Y, ctx.Z) && !ctx.Environment.IsRaining(ctx.X + 1, ctx.Y, ctx.Z) &&
                                                               !ctx.Environment.IsRaining(ctx.X, ctx.Y, ctx.Z - 1) && !ctx.Environment.IsRaining(ctx.X, ctx.Y, ctx.Z + 1)))
        {
            int fireAge = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            if (fireAge < 15)
            {
                ctx.WorldWrite.SetBlockMetaWithoutNotifyingNeighbors(ctx.X, ctx.Y, ctx.Z, fireAge + ctx.Random.NextInt(3) / 2);
            }

            ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
            if (!isOnNetherrack && !areBlocksAroundFlammable(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z))
            {
                if (!ctx.WorldRead.shouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) || fireAge > 3)
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }
            }
            else if (!isOnNetherrack && !isFlammable(ctx.WorldRead, ctx.X, ctx.Y - 1, ctx.Z) && fireAge == 15 && ctx.Random.NextInt(4) == 0)
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
            else
            {
                trySpreadingFire(ctx.WorldRead, ctx.X + 1, ctx.Y, ctx.Z, 300, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldRead, ctx.X - 1, ctx.Y, ctx.Z, 300, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldRead, ctx.X, ctx.Y - 1, ctx.Z, 250, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldRead, ctx.X, ctx.Y + 1, ctx.Z, 250, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z - 1, 300, ctx.Random, fireAge);
                trySpreadingFire(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z + 1, 300, ctx.Random, fireAge);

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

                                int burnChance = getBurnChance(ctx.WorldRead, checkX, checkZ, checkY);
                                if (burnChance > 0)
                                {
                                    int var13 = (burnChance + 40) / (fireAge + 30);
                                    if (var13 > 0 && ctx.Random.NextInt(spreadDifficulty) <= var13 && (!ctx.WorldRead.isRaining() || !ctx.WorldRead.isRaining(checkX, checkZ, checkY)) && !ctx.WorldRead.isRaining(checkX - 1, checkZ, z) &&
                                        !ctx.WorldRead.isRaining(checkX + 1, checkZ, checkY) && !ctx.WorldRead.isRaining(checkX, checkZ, checkY - 1) && !ctx.WorldRead.isRaining(checkX, checkZ, checkY + 1))
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

    private void trySpreadingFire(IBlockReader read, IBlockWrite write, EnvironmentManager environment, int x, int y, int z, int spreadFactor, JavaRandom random, int currentAge)
    {
        int targetSpreadChance = _spreadChances[read.GetBlockId(x, y, z)];
        if (random.NextInt(spreadFactor) < targetSpreadChance)
        {
            bool isTnt = read.GetBlockId(x, y, z) == TNT.id;
            if (random.NextInt(currentAge + 10) < 5 && !environment.IsRainingAt(x, y, z))
            {
                int newFireAge = currentAge + random.NextInt(5) / 4;
                if (newFireAge > 15)
                {
                    newFireAge = 15;
                }

                write.SetBlock(x, y, z, id, newFireAge);
            }
            else
            {
                write.SetBlock(x, y, z, 0);
            }

            if (isTnt)
            {
                TNT.onMetadataChange(read, x, y, z, 1);
            }
        }
    }

    private bool areBlocksAroundFlammable(IBlockReader world, int x, int y, int z) => isFlammable(world, x + 1, y, z) ? true :
        isFlammable(world, x - 1, y, z) ? true :
        isFlammable(world, x, y - 1, z) ? true :
        isFlammable(world, x, y + 1, z) ? true :
        isFlammable(world, x, y, z - 1) ? true : isFlammable(world, x, y, z + 1);

    private int getBurnChance(World world, int x, int y, int z)
    {
        sbyte initialMax = 0;
        if (!world.isAir(x, y, z))
        {
            return 0;
        }

        int maxChance = getBurnChance(world, x + 1, y, z, initialMax);
        maxChance = getBurnChance(world, x - 1, y, z, maxChance);
        maxChance = getBurnChance(world, x, y - 1, z, maxChance);
        maxChance = getBurnChance(world, x, y + 1, z, maxChance);
        maxChance = getBurnChance(world, x, y, z - 1, maxChance);
        maxChance = getBurnChance(world, x, y, z + 1, maxChance);
        return maxChance;
    }

    public override bool hasCollision() => false;

    public override bool isFlammable(IBlockReader reader, int x, int y, int z) => _burnChances[reader.GetBlockId(x, y, z)] > 0;

    public int getBurnChance(World world, int x, int y, int z, int currentChance)
    {
        int blockBurnChance = _burnChances[world.getBlockId(x, y, z)];
        return blockBurnChance > currentChance ? blockBurnChance : currentChance;
    }

    public override bool canPlaceAt(OnPlacedEvt ctx) => ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) || areBlocksAroundFlammable(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z);

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && !areBlocksAroundFlammable(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z))
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
    }

    public override void onPlaced(OnPlacedEvt ctx)
    {
        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z) != Obsidian.id || !NetherPortal.create(ctx.WorldRead, ctx.WorldWrite, ctx.X, ctx.Y, ctx.Z))
        {
            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && !areBlocksAroundFlammable(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z))
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
            else
            {
                ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
            }
        }
    }

    public override void randomDisplayTick(OnTickEvt ctx)
    {
        if (ctx.Random.NextInt(24) == 0)
        {
            ctx.Broadcaster.PlaySoundAtPos(ctx.X + 0.5F, ctx.Y + 0.5F, ctx.Z + 0.5F, "fire.fire", 1.0F + ctx.Random.NextFloat(), ctx.Random.NextFloat() * 0.7F + 0.3F);
        }

        int particleIndex;
        float particleX;
        float particleY;
        float particleZ;
        if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && !Fire.isFlammable(ctx.WorldRead, ctx.X, ctx.Y - 1, ctx.Z))
        {
            if (Fire.isFlammable(ctx.WorldRead, ctx.X - 1, ctx.Y, ctx.Z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = ctx.X + ctx.Random.NextFloat() * 0.1F;
                    particleY = ctx.Y + ctx.Random.NextFloat();
                    particleZ = ctx.Z + ctx.Random.NextFloat();
                    ctx.Broadcaster.AddParticle("largesmoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.isFlammable(ctx.WorldRead, ctx.X + 1, ctx.Y, ctx.Z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = ctx.X + 1 - ctx.Random.NextFloat() * 0.1F;
                    particleY = ctx.Y + ctx.Random.NextFloat();
                    particleZ = ctx.Z + ctx.Random.NextFloat();
                    world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.isFlammable(world.BlocksView, x, y, z - 1))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = (float)x + random.NextFloat();
                    particleY = (float)y + random.NextFloat();
                    particleZ = (float)z + random.NextFloat() * 0.1F;
                    world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.isFlammable(world.BlocksView, x, y, z + 1))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = (float)x + random.NextFloat();
                    particleY = (float)y + random.NextFloat();
                    particleZ = z + 1 - random.NextFloat() * 0.1F;
                    world.addParticle("largesmoke", (double)particleX, (double)particleY, (double)particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.isFlammable(world.BlocksView, x, y + 1, z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = (float)x + random.NextFloat();
                    particleY = y + 1 - random.NextFloat() * 0.1F;
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
