using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockTorchTests
{
    private sealed class TestPlayer : EntityPlayer
    {
        public TestPlayer(IWorldContext world) : base(world)
        {
        }

        public override EntityType Type => EntityRegistry.Player;

        public override void spawn()
        {
        }
    }

    [Fact]
    public void GetCollisionShape_ReturnsNull()
    {
        FakeWorldContext world = new();
        Assert.Null(Block.Torch.getCollisionShape(world.Reader, world.Entities, 0, 64, 0));
    }

    [Fact]
    public void IsOpaque_IsFalse()
    {
        Assert.False(Block.Torch.isOpaque());
    }

    [Fact]
    public void IsFullCube_IsFalse()
    {
        Assert.False(Block.Torch.isFullCube());
    }

    [Fact]
    public void GetRenderType_IsTorch()
    {
        Assert.Equal(BlockRendererType.Torch, Block.Torch.getRenderType());
    }

    [Theory]
    [InlineData(0, 64, 0, 0, 64, -1, true)]
    [InlineData(0, 64, 0, 0, 64, 1, true)]
    [InlineData(0, 64, 0, -1, 64, 0, true)]
    [InlineData(0, 64, 0, 1, 64, 0, true)]
    [InlineData(0, 64, 0, 0, 63, 0, true)]
    [InlineData(0, 64, 0, 0, 64, 0, false)]
    public void CanPlaceAt_RequiresNeighborOrFloor(int tx, int ty, int tz, int sx, int sy, int sz, bool expectTrue)
    {
        FakeWorldContext world = new();
        if (sx != tx || sy != ty || sz != tz)
        {
            world.ReaderWriter.SetInitial(sx, sy, sz, Block.Stone.id);
        }

        bool ok = Block.Torch.canPlaceAt(new CanPlaceAtContext(world, Side.Up, tx, ty, tz));
        Assert.Equal(expectTrue, ok);
    }

    [Fact]
    public void CanPlaceAt_AllowsFenceBelowWithoutOpaqueNeighbor()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Fence.id);
        Assert.True(Block.Torch.canPlaceAt(new CanPlaceAtContext(world, Side.Up, 0, 64, 0)));
    }

    [Fact]
    public void OnPlaced_DownWithCeilingAndTwoSideWalls_SelectsWestWallWhenPlacerEastOfGap()
    {
        FakeWorldContext world = new();
        int x = 0;
        int y = 65;
        int z = 0;
        world.ReaderWriter.SetInitial(x, y + 1, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x - 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x + 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Torch.id, 0);

        // Placer east of the cell, looking into the gap (−X): wall torch faces is west (meta 1).
        TestPlayer player = new(world)
        {
            x = x + 8.0,
            z = z + 0.2,
            yaw = 90f,
            pitch = -35f
        };

        Block.Torch.onPlaced(new OnPlacedEvent(world, player, Side.Down, Side.Down, x, y, z));

        Assert.Equal(1, world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_DownWithCeilingAndTwoSideWalls_SelectsEastWallWhenPlacerWestOfGap()
    {
        FakeWorldContext world = new();
        int x = 0;
        int y = 65;
        int z = 0;
        world.ReaderWriter.SetInitial(x, y + 1, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x - 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x + 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Torch.id, 0);

        TestPlayer player = new(world)
        {
            x = x - 8.0,
            z = z + 0.2,
            yaw = 270f,
            pitch = -35f
        };

        Block.Torch.onPlaced(new OnPlacedEvent(world, player, Side.Down, Side.Down, x, y, z));

        Assert.Equal(2, world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_DownWithCeilingNorthSouthWalls_PrefersNorthWallWhenPlacerSouthOfGap()
    {
        FakeWorldContext world = new();
        int x = 0;
        int y = 65;
        int z = 0;
        world.ReaderWriter.SetInitial(x, y + 1, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z - 1, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z + 1, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Torch.id, 0);

        TestPlayer player = new(world)
        {
            x = x + 0.2,
            z = z + 6.0,
            yaw = 180f,
            pitch = -35f
        };

        Block.Torch.onPlaced(new OnPlacedEvent(world, player, Side.Down, Side.Down, x, y, z));

        Assert.Equal(3, world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_DownWithCeilingNorthSouthWalls_PrefersSouthWallWhenPlacerNorthOfGap()
    {
        FakeWorldContext world = new();
        int x = 0;
        int y = 65;
        int z = 0;
        world.ReaderWriter.SetInitial(x, y + 1, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z - 1, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z + 1, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Torch.id, 0);

        TestPlayer player = new(world)
        {
            x = x + 0.2,
            z = z - 6.0,
            yaw = 0f,
            pitch = -35f
        };

        Block.Torch.onPlaced(new OnPlacedEvent(world, player, Side.Down, Side.Down, x, y, z));

        Assert.Equal(4, world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_DownWithCeilingTwoWalls_NoPlacer_UsesVanillaWestFirst()
    {
        FakeWorldContext world = new();
        int x = 0;
        int y = 65;
        int z = 0;
        world.ReaderWriter.SetInitial(x, y + 1, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x - 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x + 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Torch.id, 0);

        Block.Torch.onPlaced(new OnPlacedEvent(world, null, Side.Down, Side.Down, x, y, z));

        Assert.Equal(1, world.Reader.GetBlockMeta(x, y, z));
    }

    /// <summary>
    /// [B][A][B] along X: placer south and centered on X so distance to both side blocks matches — tie-break is vanilla (−X first).
    /// </summary>
    [Fact]
    public void OnPlaced_DownEastWestEquidistantPlacer_TieBreaksToWestMeta()
    {
        FakeWorldContext world = new();
        int x = 0;
        int y = 65;
        int z = 0;
        world.ReaderWriter.SetInitial(x, y + 1, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x - 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x + 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Torch.id, 0);

        TestPlayer player = new(world)
        {
            x = x + 0.5,
            z = z + 8.0,
            yaw = 0f,
            pitch = -35f
        };

        Block.Torch.onPlaced(new OnPlacedEvent(world, player, Side.Down, Side.Down, x, y, z));

        Assert.Equal(1, world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_UpOnSolidBelow_StillFloorTorch()
    {
        FakeWorldContext world = new();
        int x = 0;
        int y = 65;
        int z = 0;
        world.ReaderWriter.SetInitial(x, y - 1, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Torch.id, 0);

        Block.Torch.onPlaced(new OnPlacedEvent(world, null, Side.Up, Side.Up, x, y, z));

        Assert.Equal(5, world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_UpWithNoFloorBelow_KeepsMetaZero()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Torch.id, 0);

        Block.Torch.onPlaced(new OnPlacedEvent(world, null, Side.Up, Side.Up, 0, 65, 0));

        Assert.Equal(0, world.Reader.GetBlockMeta(0, 65, 0));
    }

    [Theory]
    [InlineData(Side.North, 0, 65, 1, 4)]
    [InlineData(Side.South, 0, 65, -1, 3)]
    [InlineData(Side.West, 1, 65, 0, 2)]
    [InlineData(Side.East, -1, 65, 0, 1)]
    public void OnPlaced_CardinalSides_SetWallMeta(Side direction, int nx, int ny, int nz, int expectedMeta)
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(nx, ny, nz, Block.Stone.id);
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Torch.id, 0);

        Block.Torch.onPlaced(new OnPlacedEvent(world, null, direction, direction, 0, 65, 0));

        Assert.Equal(expectedMeta, world.Reader.GetBlockMeta(0, 65, 0));
    }

    [Fact]
    public void OnTick_MetaZeroOnlyWestNeighbor_ResolvesToWestWall()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(-1, 65, 0, Block.Stone.id);
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Torch.id, 0);

        Block.Torch.onTick(new OnTickEvent(world, 0, 65, 0, 0, Block.Torch.id));

        Assert.Equal(1, world.Reader.GetBlockMeta(0, 65, 0));
    }

    [Fact]
    public void OnTick_MetaZeroNoSupport_RemovesTorch()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Torch.id, 0);

        Block.Torch.onTick(new OnTickEvent(world, 0, 65, 0, 0, Block.Torch.id));

        Assert.Equal(0, world.Reader.GetBlockId(0, 65, 0));
    }

    [Fact]
    public void OnTick_MetaNonZero_DoesNotReResolveFromZero()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(-1, 65, 0, Block.Stone.id);
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Torch.id, 2);

        Block.Torch.onTick(new OnTickEvent(world, 0, 65, 0, 2, Block.Torch.id));

        Assert.Equal(2, world.Reader.GetBlockMeta(0, 65, 0));
    }

    [Fact]
    public void NeighborUpdate_WallTorchLosesSupportingSide_Drops()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(-1, 65, 0, Block.Stone.id);
        world.ReaderWriter.SetInitial(1, 65, 0, Block.Stone.id);
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Torch.id, 1);

        world.ReaderWriter.SetInitial(-1, 65, 0, 0);

        Block.Torch.neighborUpdate(new OnTickEvent(world, 0, 65, 0, 1, Block.Torch.id));

        Assert.Equal(0, world.Reader.GetBlockId(0, 65, 0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(6)]
    public void Raycast_SegmentThroughBounds_HitsTorch(int meta)
    {
        FakeWorldContext world = new();
        int x = 2;
        int y = 64;
        int z = 3;
        world.ReaderWriter.SetInitial(x, y, z, Block.Torch.id, meta);

        (Vec3D start, Vec3D end) = meta switch
        {
            1 => (new Vec3D(x - 0.05, y + 0.5, z + 0.5), new Vec3D(x + 0.2, y + 0.5, z + 0.5)),
            2 => (new Vec3D(x + 1.05, y + 0.5, z + 0.5), new Vec3D(x + 0.85, y + 0.5, z + 0.5)),
            3 => (new Vec3D(x + 0.5, y + 0.5, z - 0.05), new Vec3D(x + 0.5, y + 0.5, z + 0.2)),
            4 => (new Vec3D(x + 0.5, y + 0.5, z + 1.05), new Vec3D(x + 0.5, y + 0.5, z + 0.85)),
            _ => (new Vec3D(x + 0.5, y + 1.05, z + 0.5), new Vec3D(x + 0.5, y + 0.05, z + 0.5)),
        };

        HitResult hit = Block.Torch.raycast(world.Reader, world.Entities, x, y, z, start, end);

        Assert.NotEqual(HitResultType.MISS, hit.Type);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void RandomDisplayTick_AllMetas_DoesNotThrow(int meta)
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Torch.id, meta);

        Exception? ex = Record.Exception(() => Block.Torch.randomDisplayTick(new OnTickEvent(world, 0, 64, 0, meta, Block.Torch.id)));

        Assert.Null(ex);
    }
}
