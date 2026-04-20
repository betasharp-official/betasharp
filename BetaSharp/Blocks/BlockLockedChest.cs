using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockLockedChest : Block
{
    public BlockLockedChest(int id) : base(id, Material.Wood) => TextureId = BlockTextures.ChestSingleSide;

    public override int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (side is Side.Up or Side.Down) return BlockTextures.ChestTopBottom;

        int blockNorth = iBlockReader.GetBlockId(x, y, z - 1);
        int blockSouth = iBlockReader.GetBlockId(x, y, z + 1);
        int blockWest = iBlockReader.GetBlockId(x - 1, y, z);
        int blockEast = iBlockReader.GetBlockId(x + 1, y, z);

        Side facing = Side.South;
        if (BlocksOpaque[blockNorth] && !BlocksOpaque[blockSouth]) facing = Side.South;
        if (BlocksOpaque[blockSouth] && !BlocksOpaque[blockNorth]) facing = Side.North;
        if (BlocksOpaque[blockWest] && !BlocksOpaque[blockEast]) facing = Side.East;
        if (BlocksOpaque[blockEast] && !BlocksOpaque[blockWest]) facing = Side.West;
        return side == facing ? BlockTextures.ChestSingleFront : BlockTextures.ChestSingleSide;
    }

    public override int GetTexture(Side side) => side switch
    {
        Side.Up or Side.Down => BlockTextures.ChestTopBottom,
        Side.South => BlockTextures.ChestSingleFront,
        _ => BlockTextures.ChestSingleSide
    };

    public override bool CanPlaceAt(CanPlaceAtContext context) => true;

    public override void OnTick(OnTickEvent @event) => @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
}
