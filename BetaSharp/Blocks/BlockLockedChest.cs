using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockLockedChest : Block
{
    public BlockLockedChest(int id) : base(id, Material.Wood) => textureId = 26;

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (side is 1 or 0)
        {
            return textureId - 1;
        }

        int blockNorth = iBlockReader.GetBlockId(x, y, z - 1);
        int blockSouth = iBlockReader.GetBlockId(x, y, z + 1);
        int blockWest = iBlockReader.GetBlockId(x - 1, y, z);
        int blockEast = iBlockReader.GetBlockId(x + 1, y, z);

        sbyte facing = 3;
        if (BlocksOpaque[blockNorth] && !BlocksOpaque[blockSouth])
        {
            facing = 3;
        }

        if (BlocksOpaque[blockSouth] && !BlocksOpaque[blockNorth])
        {
            facing = 2;
        }

        if (BlocksOpaque[blockWest] && !BlocksOpaque[blockEast])
        {
            facing = 5;
        }

        if (BlocksOpaque[blockEast] && !BlocksOpaque[blockWest])
        {
            facing = 4;
        }

        return side == facing ? textureId + 1 : textureId;
    }

    public override int getTexture(int side) => side == 1 ? textureId - 1 : side == 0 ? textureId - 1 : side == 3 ? textureId + 1 : textureId;

    public override bool canPlaceAt(CanPlaceAtContext context) => true;

    public override void onTick(OnTickEvent @event) => @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
}
