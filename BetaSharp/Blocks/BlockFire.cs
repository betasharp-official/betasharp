using BetaSharp.Blocks.Materials;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockFire : Block
{
    private const sbyte InitialMax = 0;
    private readonly int[] _burnChances = new int[256];
    private readonly int[] _spreadChances = new int[256];

    public BlockFire(int id, int textureId) : base(id, textureId, Material.Fire) => SetTickRandomly(true);

    protected override void Init()
    {
        RegisterFlammableBlock(Planks.ID, 5, 20);
        RegisterFlammableBlock(Fence.ID, 5, 20);
        RegisterFlammableBlock(WoodenStairs.ID, 5, 20);
        RegisterFlammableBlock(Log.ID, 5, 5);
        RegisterFlammableBlock(Leaves.ID, 30, 60);
        RegisterFlammableBlock(Bookshelf.ID, 30, 20);
        RegisterFlammableBlock(TNT.ID, 15, 100);
        RegisterFlammableBlock(Grass.ID, 60, 100);
        RegisterFlammableBlock(Wool.ID, 30, 60);
    }

    private void RegisterFlammableBlock(int block, int burnChange, int spreadChance)
    {
        _burnChances[block] = burnChange;
        _spreadChances[block] = spreadChance;
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Fire;

    public override int GetDroppedItemCount() => 0;

    public override int GetTickRate() => 40;

    public override void OnTick(OnTickEvent @event)
    {
        if (!@event.World.Rules.GetBool(DefaultRules.DoFireTick))
        {
            return;
        }


        bool isOnNetherrack = @event.World.Reader.GetBlockId(@event.X, @event.Y - 1, @event.Z) == Netherrack.ID;
        if (!CanPlaceAt(new CanPlaceAtContext(@event.World, 0, @event.X, @event.Y, @event.Z)))
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }

        if (isOnNetherrack ||
            !@event.World.Environment.IsRaining ||
            (!@event.World.Environment.IsRainingAt(@event.X, @event.Y, @event.Z) &&
             !@event.World.Environment.IsRainingAt(@event.X - 1, @event.Y, @event.Z) &&
             !@event.World.Environment.IsRainingAt(@event.X + 1, @event.Y, @event.Z) &&
             !@event.World.Environment.IsRainingAt(@event.X, @event.Y, @event.Z - 1) &&
             !@event.World.Environment.IsRainingAt(@event.X, @event.Y, @event.Z + 1)))
        {
            int fireAge = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            if (fireAge < 15)
            {
                @event.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, fireAge + @event.World.Random.NextInt(3) / 2);
            }

            @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, ID, GetTickRate());
            if (!isOnNetherrack && !AreBlocksAroundFlammable(@event.World.Reader, @event.X, @event.Y, @event.Z))
            {
                if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z) || fireAge > 3)
                {
                    @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                }
            }
            else if (!isOnNetherrack && !IsFlammable(@event.World.Reader, @event.X, @event.Y - 1, @event.Z) && fireAge == 15 && @event.World.Random.NextInt(4) == 0)
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            }
            else
            {
                TrySpreadingFire(@event.World, @event.X + 1, @event.Y, @event.Z, 300, @event.World.Random, fireAge);
                TrySpreadingFire(@event.World, @event.X - 1, @event.Y, @event.Z, 300, @event.World.Random, fireAge);
                TrySpreadingFire(@event.World, @event.X, @event.Y - 1, @event.Z, 250, @event.World.Random, fireAge);
                TrySpreadingFire(@event.World, @event.X, @event.Y + 1, @event.Z, 250, @event.World.Random, fireAge);
                TrySpreadingFire(@event.World, @event.X, @event.Y, @event.Z - 1, 300, @event.World.Random, fireAge);
                TrySpreadingFire(@event.World, @event.X, @event.Y, @event.Z + 1, 300, @event.World.Random, fireAge);

                for (int checkX = @event.X - 1; checkX <= @event.X + 1; ++checkX)
                {
                    for (int checkZ = @event.Z - 1; checkZ <= @event.Z + 1; ++checkZ)
                    {
                        for (int checkY = @event.Y - 1; checkY <= @event.Y + 4; ++checkY)
                        {
                            if (checkX != @event.X || checkY != @event.Y || checkZ != @event.Z)
                            {
                                int spreadDifficulty = 100;
                                if (checkY > @event.Y + 1)
                                {
                                    spreadDifficulty += (checkY - (@event.Y + 1)) * 100;
                                }

                                int burnChance = GetBurnChance(@event.World.Reader, checkX, checkY, checkZ);
                                if (burnChance > 0)
                                {
                                    int spreadThreshold = (burnChance + 40) / (fireAge + 30);
                                    if (spreadThreshold > 0 &&
                                        @event.World.Random.NextInt(spreadDifficulty) <= spreadThreshold &&
                                        (!@event.World.Environment.IsRaining || !@event.World.Environment.IsRainingAt(checkX, checkY, checkZ)) &&
                                        !@event.World.Environment.IsRainingAt(checkX - 1, checkY, checkZ) &&
                                        !@event.World.Environment.IsRainingAt(checkX + 1, checkY, checkZ) &&
                                        !@event.World.Environment.IsRainingAt(checkX, checkY - 1, checkZ) &&
                                        !@event.World.Environment.IsRainingAt(checkX, checkY + 1, checkZ))
                                    {
                                        int spreadChance = fireAge + @event.World.Random.NextInt(5) / 4;
                                        if (spreadChance > 15)
                                        {
                                            spreadChance = 15;
                                        }

                                        @event.World.Writer.SetBlock(checkX, checkY, checkZ, ID, spreadChance);
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
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
    }

    private void TrySpreadingFire(IWorldContext level, int x, int y, int z, int spreadFactor, JavaRandom random, int currentAge)
    {
        int targetSpreadChance = _spreadChances[level.Reader.GetBlockId(x, y, z)];
        if (random.NextInt(spreadFactor) < targetSpreadChance)
        {
            bool isTnt = level.Reader.GetBlockId(x, y, z) == TNT.ID;
            if (random.NextInt(currentAge + 10) < 5 && !level.Environment.IsRainingAt(x, y, z))
            {
                int newFireAge = currentAge + random.NextInt(5) / 4;
                if (newFireAge > 15)
                {
                    newFireAge = 15;
                }

                level.Writer.SetBlock(x, y, z, ID, newFireAge);
            }
            else
            {
                level.Writer.SetBlock(x, y, z, 0);
            }

            if (isTnt)
            {
                TNT.OnMetadataChange(new OnMetadataChangeEvent(level, x, y, z, 1));
            }
        }
    }

    private bool AreBlocksAroundFlammable(IBlockReader world, int x, int y, int z) => IsFlammable(world, x + 1, y, z) ||
                                                                                      IsFlammable(world, x - 1, y, z) ||
                                                                                      IsFlammable(world, x, y - 1, z) ||
                                                                                      IsFlammable(world, x, y + 1, z) ||
                                                                                      IsFlammable(world, x, y, z - 1) ||
                                                                                      IsFlammable(world, x, y, z + 1);

    private int GetBurnChance(IBlockReader world, int x, int y, int z)
    {
        if (!world.IsAir(x, y, z))
        {
            return 0;
        }

        int maxChance = GetBurnChance(world, x + 1, y, z, InitialMax);
        maxChance = GetBurnChance(world, x - 1, y, z, maxChance);
        maxChance = GetBurnChance(world, x, y - 1, z, maxChance);
        maxChance = GetBurnChance(world, x, y + 1, z, maxChance);
        maxChance = GetBurnChance(world, x, y, z - 1, maxChance);
        maxChance = GetBurnChance(world, x, y, z + 1, maxChance);
        return maxChance;
    }

    public override bool HasCollision() => false;

    public override bool IsFlammable(IBlockReader reader, int x, int y, int z) => _burnChances[reader.GetBlockId(x, y, z)] > 0;

    public int GetBurnChance(IBlockReader world, int x, int y, int z, int currentChance)
    {
        int blockBurnChance = _burnChances[world.GetBlockId(x, y, z)];
        return blockBurnChance > currentChance ? blockBurnChance : currentChance;
    }

    public override bool CanPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z) || AreBlocksAroundFlammable(context.World.Reader, context.X, context.Y, context.Z);

    public override void NeighborUpdate(OnTickEvent ctx)
    {
        if (!ctx.World.Reader.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && !AreBlocksAroundFlammable(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z))
        {
            ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
    }

    public override void OnPlaced(OnPlacedEvent ctx)
    {
        if (ctx.World.Reader.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z) == Obsidian.ID && BlockPortal.Create(ctx.World.Reader, ctx.World.Writer, ctx.X, ctx.Y, ctx.Z))
        {
            return;
        }

        if (!ctx.World.Reader.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && !AreBlocksAroundFlammable(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z))
        {
            ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
        else
        {
            ctx.World.TickScheduler.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, ID, GetTickRate());
        }
    }

    public override void RandomDisplayTick(OnTickEvent ctx)
    {
        if (ctx.World.Random.NextInt(24) == 0)
        {
            ctx.World.Broadcaster.PlaySoundAtPos(ctx.X + 0.5F, ctx.Y + 0.5F, ctx.Z + 0.5F, "fire.fire", 1.0F + Random.Shared.NextSingle(), Random.Shared.NextSingle() * 0.7F + 0.3F);
        }

        int particleIndex;
        float particleX;
        float particleY;
        float particleZ;
        if (!ctx.World.Reader.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && !Fire.IsFlammable(ctx.World.Reader, ctx.X, ctx.Y - 1, ctx.Z))
        {
            if (Fire.IsFlammable(ctx.World.Reader, ctx.X - 1, ctx.Y, ctx.Z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = ctx.X + Random.Shared.NextSingle() * 0.1F;
                    particleY = ctx.Y + Random.Shared.NextSingle();
                    particleZ = ctx.Z + Random.Shared.NextSingle();
                    ctx.World.Broadcaster.AddParticle("largesmoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.IsFlammable(ctx.World.Reader, ctx.X + 1, ctx.Y, ctx.Z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = ctx.X + 1 - Random.Shared.NextSingle() * 0.1F;
                    particleY = ctx.Y + Random.Shared.NextSingle();
                    particleZ = ctx.Z + Random.Shared.NextSingle();
                    ctx.World.Broadcaster.AddParticle("largesmoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.IsFlammable(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z - 1))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = ctx.X + Random.Shared.NextSingle();
                    particleY = ctx.Y + Random.Shared.NextSingle();
                    particleZ = ctx.Z + Random.Shared.NextSingle() * 0.1F;
                    ctx.World.Broadcaster.AddParticle("largesmoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.IsFlammable(ctx.World.Reader, ctx.X, ctx.Y, ctx.Z + 1))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = ctx.X + Random.Shared.NextSingle();
                    particleY = ctx.Y + Random.Shared.NextSingle();
                    particleZ = ctx.Z + 1 - Random.Shared.NextSingle() * 0.1F;
                    ctx.World.Broadcaster.AddParticle("largesmoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                }
            }

            if (Fire.IsFlammable(ctx.World.Reader, ctx.X, ctx.Y + 1, ctx.Z))
            {
                for (particleIndex = 0; particleIndex < 2; ++particleIndex)
                {
                    particleX = ctx.X + Random.Shared.NextSingle();
                    particleY = ctx.Y + 1 - Random.Shared.NextSingle() * 0.1F;
                    particleZ = ctx.Z + Random.Shared.NextSingle();
                    ctx.World.Broadcaster.AddParticle("largesmoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                }
            }
        }
        else
        {
            for (particleIndex = 0; particleIndex < 3; ++particleIndex)
            {
                particleX = ctx.X + Random.Shared.NextSingle();
                particleY = ctx.Y + Random.Shared.NextSingle() * 0.5F + 0.5F;
                particleZ = ctx.Z + Random.Shared.NextSingle();
                ctx.World.Broadcaster.AddParticle("largesmoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }
        }
    }
}
