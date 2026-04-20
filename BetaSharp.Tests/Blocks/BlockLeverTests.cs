using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockLeverTests
{
    [Fact]
    public void OnUse_TogglesPoweredBitInMetadata()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(-1, 64, 0, Block.Stone.ID); // support for facing=1
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Lever.ID, 1);

        bool firstUse = Block.Lever.OnUse(new OnUseEvent(world, null!, 0, 64, 0));
        int poweredMeta = world.Reader.GetBlockMeta(0, 64, 0);

        bool secondUse = Block.Lever.OnUse(new OnUseEvent(world, null!, 0, 64, 0));
        int unpoweredMeta = world.Reader.GetBlockMeta(0, 64, 0);

        Assert.True(firstUse);
        Assert.True(secondUse);
        Assert.Equal(9, poweredMeta); // facing 1 + powered bit
        Assert.Equal(1, unpoweredMeta); // toggled back
    }

    [Fact]
    public void PoweredLever_StrongPowersAttachedSide()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(-1, 64, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Lever.ID, 9); // facing=1, powered

        bool strongPowerOnAttachedSide = Block.Lever.IsStrongPoweringSide(world.Reader, 0, 64, 0, 5);
        bool strongPowerOnOtherSide = Block.Lever.IsStrongPoweringSide(world.Reader, 0, 64, 0, 4);

        Assert.True(strongPowerOnAttachedSide);
        Assert.False(strongPowerOnOtherSide);
    }

    [Fact]
    public void NeighborUpdate_WhenSupportRemoved_DropsLever()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(-1, 64, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Lever.ID, 1); // attached to west block

        world.ReaderWriter.SetBlock(-1, 64, 0, 0);
        Block.Lever.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 1, Block.Stone.ID));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }
}
